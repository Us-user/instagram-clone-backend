# Промпт: Прямой эфир (Live Streaming) — модуль бэкенда Instagram-клона

> Это **дополнение** к уже готовому бэкенду (ASP.NET Core 8 + EF Core + PostgreSQL + Identity + JWT + SignalR), собранному по основному ТЗ (`instagram-backend-prompt.md`) и расширению (`instagram-backend-features-prompt.md`).
> Модуль встраивается **в тот же проект** — те же слои Domain / Infrastructure / WebApi, тот же `Response<T>`, тот же JWT, те же миграции. Отдельный репозиторий не нужен.
> Видео **не проходит через бэкенд** — им занимается внешний WebRTC-сервер (**LiveKit**). Бэкенд управляет всей логикой вокруг: эфиры, доступ, зрители, комменты, гости, статистика.

---

Ты — senior .NET backend разработчик. Добавь в существующий бэкенд Instagram-клона модуль прямых эфиров. Сохраняй архитектуру и стиль основы: слои Domain / Infrastructure / WebApi, единый формат ответа `Response<T>` / `PagedResponse<T>`, ID текущего юзера — из JWT claims, обработка ошибок через существующий middleware, валидация через FluentValidation. Создай миграции и обнови сид.

---

## Схема работы

```
Твой C# бэкенд ──HTTP/Server API──> LiveKit (Cloud или self-hosted Docker)
      │                                   ↑
      │                                   │ видео/аудио (WebRTC)
      └──SignalR──> Клиент ──────────────┘
         (комменты, зрители, заявки)
```

Видео идёт **напрямую** между клиентом и LiveKit, минуя бэкенд. Бэкенд раздаёт токены доступа и ведёт всю бизнес-логику — нагрузка на него минимальная.

---

## 1. Абстракция провайдера

Создать `Infrastructure/Services/Streaming/`:

```csharp
public interface IStreamingProvider
{
    Task<string> CreateRoomAsync(string roomName);
    Task<string> GenerateTokenAsync(string roomName, string userId, string userName, ParticipantRole role);
    Task UpdateParticipantRoleAsync(string roomName, string userId, ParticipantRole role);
    Task RemoveParticipantAsync(string roomName, string userId);
    Task CloseRoomAsync(string roomName);
}

public enum ParticipantRole
{
    Subscriber = 0, // только смотрит
    Publisher  = 1  // вещает (хост или одобренный гость)
}
```

**Реализации:**
- **`LiveKitStreamingProvider`** — основная. LiveKit Server SDK для .NET (или прямые вызовы Server API + генерация JWT-токенов доступа с grants: `roomJoin`, `canPublish`, `canSubscribe`).
- **`FakeStreamingProvider`** — заглушка для тестов и локальной разработки без видео (возвращает фиктивные токены), чтобы бизнес-логику можно было проверять изолированно.

Выбор реализации — через конфиг (`Streaming:Provider`), регистрация в DI.

---

## 2. Конфигурация (`appsettings.json`)

```json
"Streaming": {
  "Provider": "LiveKit",
  "LiveKit": {
    "Url": "wss://your-project.livekit.cloud",
    "ApiKey": "...",
    "ApiSecret": "...",
    "TokenLifetimeMinutes": 360
  },
  "MaxGuests": 3,
  "MaxCommentLength": 200
}
```

Один и тот же код работает и с **LiveKit Cloud**, и с **self-hosted** — меняются только `Url` и ключи. В README добавить опциональный `docker-compose.yml` для локального self-hosted LiveKit + инструкцию.

---

## 3. Сущности

| Сущность | Поля |
|---|---|
| **LiveStream** | Id, UserId (FK — хост), Title (nullable), RoomName (unique, напр. `live_{guid}`), Status (enum: Live=0, Ended=1), Audience (enum: All=0, CloseFriends=1), StartedAt, EndedAt (nullable), ViewersPeak (int), ViewersTotal (int), CommentsCount (int), LikesCount (int), SavedToStory (bool), RecordingUrl (string?) |
| **LiveViewer** | Id, LiveStreamId (FK), UserId (FK), JoinedAt, LeftAt (nullable), WatchDurationSeconds (int) — при повторном заходе новая запись; «уникальные зрители» = `DISTINCT UserId` |
| **LiveComment** | Id, LiveStreamId (FK), UserId (FK), Text, CreatedAt, IsPinned (bool), IsDeleted (bool) |
| **LiveLike** | Id, LiveStreamId (FK), UserId (FK), CreatedAt — «сердечки», можно многократно (не тумблер), поток событий |
| **LiveGuestRequest** | Id, LiveStreamId (FK), UserId (FK), Status (enum: Pending=0, Approved=1, Declined=2, Cancelled=3, Removed=4), RequestedAt, RespondedAt (nullable) |
| **LiveBan** | Id, LiveStreamId (FK), UserId (FK), BannedAt — кик без возможности вернуться |

**Индексы:** `LiveStream.UserId`, `LiveStream.Status`, `LiveViewer.(LiveStreamId, UserId)`, `LiveComment.LiveStreamId`, `LiveGuestRequest.(LiveStreamId, Status)`.

---

## 4. Гости в эфире (как в Instagram)

- Лимит **до 3 гостей одновременно** (конфиг `MaxGuests: 3`; всего в эфире 4 участника включая хоста). Заявки сверх лимита остаются `Pending` в очереди — при выходе гостя хост может одобрить следующую.

**Флоу:**
1. Зритель шлёт заявку → `LiveGuestRequest` со `Status=Pending` → real-time событие хосту (`GuestRequestReceived`).
2. Хост одобряет → проверка лимита → `IStreamingProvider.UpdateParticipantRoleAsync(role: Publisher)` → зритель начинает вещать → событие всем (`GuestJoined`), статус `Approved`.
3. Хост отклоняет → `Declined` → событие заявителю (`GuestRequestDeclined`).
4. Хост убирает гостя → роль понижается до `Subscriber` → `Removed` → событие всем (`GuestLeft`).
5. Зритель может отменить свою заявку → `Cancelled`.

**Заявку нельзя подать**, если: ты хост, уже гость, забанен в этом эфире, заблокирован хостом (или заблокировал его), уже есть активная Pending-заявка.

При завершении эфира все гости автоматически отключаются.

---

## 5. Эндпоинты (`LiveStreamController`)

### Управление эфиром (хост)
- `POST /Live/start` — body `{ title, audience }`. Создаёт `LiveStream` (Status=Live), комнату у провайдера, возвращает `{ streamId, roomName, token, serverUrl }` (токен с ролью Publisher). Уведомление подписчикам (§8). Запретить второй активный эфир у одного юзера.
- `POST /Live/end?streamId` — только хост. Status=Ended, `EndedAt`, закрыть комнату, зафиксировать `WatchDurationSeconds` всем активным зрителям, событие `StreamEnded` всем. Вернуть итоговую статистику.
- `PUT /Live/update-title?streamId` — только хост.

### Просмотр (зритель)
- `GET /Live/get-active?PageNumber&PageSize` — активные эфиры тех, на кого подписан (+ учёт приватности / блокировок / close friends).
- `GET /Live/get-stream-by-id?streamId` — инфо об эфире (хост, заголовок, счётчик зрителей, статус, гости).
- `POST /Live/join?streamId` — присоединиться: проверки доступа (не забанен, не заблокирован, аудитория подходит), создать `LiveViewer`, вернуть `{ token, serverUrl }` (роль Subscriber), обновить `ViewersPeak`/`ViewersTotal`, события `ViewerJoined` + `ViewerCount`.
- `POST /Live/leave?streamId` — выход: `LeftAt`, посчитать `WatchDurationSeconds`, события `ViewerLeft` + `ViewerCount`.

### Гости
- `POST /Live/request-guest?streamId` — подать заявку.
- `DELETE /Live/cancel-guest-request?streamId` — отменить свою заявку.
- `GET /Live/get-guest-requests?streamId` — только хост: список Pending.
- `POST /Live/approve-guest?requestId` — только хост: одобрить (проверка лимита `MaxGuests`).
- `POST /Live/decline-guest?requestId` — только хост.
- `POST /Live/remove-guest?streamId&userId` — только хост: убрать гостя из эфира.
- `GET /Live/get-active-guests?streamId` — текущие гости.

### Комментарии и реакции
- `POST /Live/add-comment?streamId` — body `{ text }`. Валидация длины (`MaxCommentLength`), проверка бана/блокировки. Сохранить + real-time всем (`NewComment`).
- `DELETE /Live/delete-comment?commentId` — автор коммента или хост (soft-delete `IsDeleted`).
- `POST /Live/pin-comment?commentId` — только хост, закрепить (одновременно закреплён один).
- `GET /Live/get-comments?streamId&PageNumber&PageSize` — история комментов (догрузка / после эфира).
- `POST /Live/send-like?streamId` — «сердечко», многократно; троттлинг (не чаще N в секунду на юзера), инкремент счётчика, событие `NewLike`.

### Модерация
- `POST /Live/ban-viewer?streamId&userId` — только хост: кикнуть и запретить возвращаться (`RemoveParticipantAsync` + запись `LiveBan`).
- `DELETE /Live/unban-viewer?streamId&userId` — только хост.
- `GET /Live/get-viewers?streamId&PageNumber&PageSize` — только хост: кто сейчас смотрит.

### Статистика
- `GET /Live/get-stats?streamId` — только хост.
  - **Live-режим:** текущее число зрителей, пик, всего уникальных, комментов, лайков, гостей.
  - **После эфира:** длительность эфира, пик, всего уникальных зрителей, **средняя длительность просмотра**, суммарное время просмотра, топ-комментаторы, число заявок в гости.
- `GET /Live/get-my-streams?PageNumber&PageSize` — история моих эфиров со статистикой.

### После эфира
- `POST /Live/save-to-story?streamId` — сохранить завершённый эфир в сторис (если есть `RecordingUrl`), флаг `SavedToStory`.

---

## 6. Real-time (`LiveHub`, SignalR)

Группа = `live_{streamId}`. При join — добавлять в группу, при leave/disconnect — убирать.

**События сервер → клиенты:**
- `ViewerJoined { userId, userName, avatar }`, `ViewerLeft { userId }`, `ViewerCount { count }`
- `NewComment { id, userId, userName, avatar, isVerified, text, createdAt }`
- `NewLike { userId }` — для анимации сердечек
- `CommentPinned { commentId }`, `CommentDeleted { commentId }`
- `GuestRequestReceived { requestId, userId, userName, avatar }` — **только хосту**
- `GuestRequestDeclined { requestId }` — только заявителю
- `GuestApproved { userId, userName }` / `GuestJoined` / `GuestLeft { userId }` — всем
- `ViewerBanned { userId }` — забаненному + всем (убрать из списка)
- `StreamEnded { streamId, stats }` — всем
- `StreamStarted { streamId, hostUserId, title }` — подписчикам хоста (§8)

**Обработка disconnect:** при обрыве соединения зрителя считать это выходом (проставить `LeftAt`, пересчитать счётчик), но с грейс-периодом ~30 сек на переподключение, чтобы моргание сети не ломало статистику.

---

## 7. Вебхуки от LiveKit

- Эндпоинт `POST /Live/webhook` — `[AllowAnonymous]`, но с **обязательной проверкой подписи** LiveKit (валидировать заголовок авторизации / подпись, отклонять неподписанные).
- Обрабатывать события: `participant_joined`, `participant_left`, `room_finished`, `track_published` — синхронизировать состояние (например, если хост потерял связь навсегда — автоматически завершить эфир по `room_finished`).
- **Идемпотентность:** одно и то же событие может прийти дважды — не дублировать записи.

---

## 8. Интеграция с существующими модулями

- **Уведомления (§2 расширения):** добавить в `NotificationType` значения `LiveStarted` (подписчикам при старте эфира — не слать заблокированным и тем, кто не в аудитории) и `LiveGuestRequest` (хосту). Пуш через существующий `NotificationHub`.
- **Close friends (§9 расширения):** `Audience=CloseFriends` → эфир виден только близким друзьям хоста.
- **Приватность (§6 расширения):** приватный аккаунт → эфир доступен только `Accepted`-подписчикам.
- **Блокировки (§6 расширения):** заблокированные не видят эфир, не могут зайти, комментировать, подавать заявки — фильтровать во всех выдачах.
- **Верификация (§10 расширения):** возвращать `isVerified` хоста и комментаторов в DTO.
- **Сессии (§14 расширения):** доступ к эфиру только с валидной активной сессией (проверка через существующий middleware).

---

## 9. Требования к качеству

1. Токены доступа генерирует **только бэкенд**, с ролью по бизнес-логике; клиенту никогда не отдавать `ApiSecret`.
2. Все проверки доступа (аудитория, блокировки, баны, лимит гостей) — **на сервере**, до выдачи токена.
3. Троттлинг: сердечки и комментарии — rate limiting на юзера (защита от спама).
4. Счётчики (`ViewersPeak`, `ViewersTotal`, `LikesCount`, `CommentsCount`) — денормализованы в `LiveStream`, обновлять инкрементами, а не `COUNT(*)` на каждый запрос.
5. Идемпотентность вебхуков и корректная обработка гонок (одновременное одобрение двух гостей при лимите).
6. Фоновая задача (`BackgroundService`): автозавершение «висящих» эфиров (Status=Live, но хост давно отключился и нет активности) — раз в N минут.
7. Все эндпоинты — в формате `Response<T>` / `PagedResponse<T>`, ошибки через существующий middleware, валидация через FluentValidation.
8. Миграции EF Core для всех новых сущностей; обновить сид.
9. Swagger: контроллер `Live` отдельным тегом, описания, Bearer-авторизация.
10. README: секция про эфиры — как получить ключи LiveKit Cloud, как поднять self-hosted через `docker-compose`, как переключать провайдера, схема потоков (видео: клиент ↔ LiveKit; логика: клиент ↔ бэкенд).

---

## Что отдать в результате

Все новые сущности + конфигурации EF, DTO, AutoMapper-профили, валидаторы, `IStreamingProvider` + обе реализации, `LiveStreamService` с бизнес-логикой, `LiveStreamController`, SignalR-хаб `LiveHub`, обработчик вебхуков, фоновую задачу автозавершения, миграции, обновлённый сид, секцию конфига, опциональный `docker-compose.yml` для self-hosted LiveKit и обновлённый README. Код должен компилироваться и работать вместе с основой и расширением без правок существующего контракта.
