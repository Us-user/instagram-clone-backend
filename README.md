# Instagram Clone — Backend

Production-ready бэкенд Instagram-клона на **C# / ASP.NET Core 8 + PostgreSQL**.
Построен строго по контракту API из [`instagram-backend-prompt.md`](./instagram-backend-prompt.md)
(пути, методы, параметры и DTO воспроизведены дословно).

> Статус: **база (Phase 0–10) + новые фичи (Phase 11–21) реализованы** — уведомления, приватность/
> блокировки, хэштеги/упоминания, ответы+лайки комментов, групповые чаты, реакции/reply/forward/
> голосовые, close friends/ответы/репост сторис, presence/typing, верификация, 2FA, Explore.
> Идёт финальный харденинг и документирование (Phase 22). Прогресс и план — в [`ROADMAP.md`](./ROADMAP.md).

## Стек
ASP.NET Core 8 Web API · EF Core 8 + Npgsql · ASP.NET Core Identity (`IdentityUser<string>`) ·
JWT Bearer · AutoMapper · FluentValidation · Swashbuckle (Swagger) ·
SignalR (чат, групповые чаты, уведомления, presence/typing).

## Архитектура (слоистая)
| Проект | Назначение |
|---|---|
| **Domain** | Entities, DTOs, Enums, Responses (`Response<T>`, `PagedResponse<T>`), Exceptions |
| **Infrastructure** | `DataContext`, EF-конфигурации, сервисы, валидаторы, миграции, seed, AutoMapper |
| **WebApi** | Controllers, `Program.cs`, DI, middleware, SignalR Hub |

Ссылки проектов: Infrastructure → Domain; WebApi → Infrastructure & Domain.
Загруженные файлы (картинки постов, аватары, файлы сообщений, сторис) хранятся в
`WebApi/wwwroot/images` и раздаются как статика; в БД сохраняется только имя файла.

## Требования
- .NET SDK 8+ (проект таргетит `net8.0`; собирается и запускается также на рантайме .NET 10 — см. `RollForward` в `WebApi.csproj`).
- PostgreSQL 14+.

## Настройка
Задать строку подключения и параметры JWT в `WebApi/appsettings.json`:

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=instagram_clone;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Issuer": "InstagramClone",
    "Audience": "InstagramCloneUsers",
    "Key": "CHANGE_ME_super_secret_signing_key_at_least_32_chars_long",
    "LifetimeMinutes": 1440
  }
}
```

- `Jwt:Key` должен быть **не короче 32 символов** (HMAC-SHA256) — обязательно замените плейсхолдер.
- Для локальной разработки переопределения можно вынести в `WebApi/appsettings.Development.json`.

## Запуск
```bash
dotnet build
dotnet run --project WebApi
```
Swagger UI: `http://localhost:<port>/swagger` (корень `/` редиректит туда) — есть кнопка
**Authorize** для передачи JWT (вставляется сам токен, без префикса `Bearer `). Swagger
включён во всех окружениях, чтобы гонять smoke-тест и на задеплоенном сервере.

## Миграции (EF Core)
Миграции и Seed применяются **автоматически при старте** приложения (`DbInitializer`) —
достаточно поднять PostgreSQL и запустить проект. Если БД недоступна, старт не падает
(ошибка логируется, Swagger всё равно поднимается). Вручную:
```bash
dotnet ef migrations add <Name> --project Infrastructure --startup-project WebApi
dotnet ef database update --project Infrastructure --startup-project WebApi
```

## Деплой на Render
В репозитории есть `Dockerfile` (multi-stage SDK 8 → ASP.NET 8) и `render.yaml` (Blueprint),
который поднимает **web-сервис (Docker)** и **PostgreSQL** сразу вместе.

1. Render Dashboard → **New → Blueprint** → выбрать этот репозиторий (при первом разе —
   авторизовать GitHub-приложение Render).
2. Render читает `render.yaml`, показывает план (1 web + 1 Postgres) → **Apply**.
3. Дождаться сборки образа и провижининга БД. URL сервиса вида
   `https://instagram-backend.onrender.com` → открыть `/swagger`.

Что делает `render.yaml` автоматически:
- `DATABASE_URL` — прокидывается из созданной БД (формат `postgresql://…`; парсится в
  `DependencyInjection.ResolveConnectionString` с включённым SSL);
- `Jwt__Key` — генерируется Render (случайный секрет ≥ 256 бит для HS256);
- `ASPNETCORE_ENVIRONMENT=Production`; health-check — `GET /health`.

Приложение слушает порт из переменной `PORT` (её задаёт Render) на `0.0.0.0`. Миграции и
Seed применяются при старте автоматически.

> ⚠️ Free-план: web-сервис засыпает при простое (первый запрос ~30 с), а free-инстанс
> Postgres истекает через 30 дней. Загруженные файлы в `wwwroot` эфемерны между деплоями
> (для постоянного хранения нужен платный Render Disk) — для демо это приемлемо.

## Тестовые аккаунты (Seed)
При первом запуске создаются роли `Admin`/`User` и тестовые пользователи с профилями:

| Логин | Пароль | Роли | Особенности |
|---|---|---|---|
| `admin` | `Admin123!` | Admin, User | верифицирован (`isVerified`) |
| `alice` | `User123!` | User | верифицирована; демо-субъект Explore |
| `bob`   | `User123!` | User | в close friends у alice |
| `carol` | `User123!` | User | заблокирована bob'ом (демо блокировки) |
| `diana` | `User123!` | User | **приватный** аккаунт; pending-запрос от carol |
| `frank` | `User123!` | User | включена **2FA** (фикс. TOTP-секрет + 3 известных backup-кода) |

Помимо аккаунтов сидируются: подписки (в т.ч. pending-запрос к приватному `diana`),
посты с лайками/комментариями/ответами/хэштегами, сторис (All + close-friends),
групповой чат «Команда проекта» с системными и текстовыми сообщениями/реакциями/reply,
уведомления, упоминания, Explore-контент вокруг `alice` и справочник локаций.

## Аутентификация и авторизация
- Аутентификация — **JWT Bearer**. Токен выдаётся `/Account/register` и `/Account/login` в поле `data`.
- **Все эндпоинты защищены по умолчанию** (fallback-политика `RequireAuthenticatedUser`).
  Открыты только `register`, `login`, `login-2fa`, `send-2fa-email`, `ForgotPassword`, `ResetPassword`
  (помечены `[AllowAnonymous]`).
- **Id текущего пользователя всегда берётся из JWT-claims**, а не из параметров запроса.
- Владелец ресурса: удалять чужие посты/комменты/сторис/сообщения нельзя (`403 Forbidden`);
  админ-действия (`/Admin/*`, `delete-user`) — только роль `Admin`; действия в группах — по роли
  участника (`Admin`/`Member`).
- Claims в токене: `userId`, `userName`, `email`, `role`.
- **Двухфакторная аутентификация (2FA):** если у аккаунта включена 2FA, `/Account/login` вместо
  JWT возвращает `{ requiresTwoFactor, twoFactorToken, methods[] }`; далее клиент вызывает
  `/Account/login-2fa` с одноразовым кодом (`Totp` / `Email` / `Backup`) и получает JWT.
  Управление — `enable-2fa` → `confirm-2fa`, `disable-2fa`, `regenerate-backup-codes`.
- **Приватность и блокировки** (Phase 12) проверяются во всех выдачах контента (ленты, поиск,
  сторис, хэштеги, чат, presence, explore): заблокированным (в любую сторону) контент/статусы не
  отдаются, приватный аккаунт виден только одобренным подписчикам.

## Формат ответа
Единая обёртка `Response<T>`:
```jsonc
{ "data": <T | null>, "errors": ["..."], "statusCode": 200 }
```
Списки с пагинацией — `PagedResponse<T>` (добавляет `pageNumber`, `pageSize`, `totalRecords`, `totalPages`).
Параметры пагинации нормализуются: `pageNumber ≥ 1`, `pageSize ∈ [1; 100]` (по умолчанию 10).

Коды ошибок (единый обработчик `ExceptionHandlingMiddleware`):

| Код | Когда |
|---|---|
| `400` | Ошибка валидации (FluentValidation) или `BadRequestException` |
| `401` | Нет/невалидный токен |
| `403` | Не владелец ресурса / нет доступа |
| `404` | Ресурс не найден |
| `500` | Необработанная ошибка сервера |

## Эндпоинты
Базовый маршрут — имя контроллера (`[Route("[controller]")]`). 🔓 — анонимный доступ,
🛡 — только `Admin`. Пагинированные эндпоинты принимают `pageNumber` / `pageSize`.

### Account — `/Account`
| Метод | Путь | Параметры |
|---|---|---|
| POST | `register` 🔓 | body `RegisterDto` |
| POST | `login` 🔓 | body `LoginDto` (при 2FA → `TwoFactorRequiredDto`) |
| POST | `login-2fa` 🔓 | body `Login2FaDto` (twoFactorToken, code, method) |
| POST | `send-2fa-email` 🔓 | body `Send2FaEmailDto` (twoFactorToken) |
| POST | `enable-2fa` | — (секрет + QR + backup-коды) |
| POST | `confirm-2fa` | `?code` (активирует 2FA) |
| POST | `disable-2fa` | `?code` (TOTP или backup) |
| POST | `regenerate-backup-codes` | — |
| DELETE | `ForgotPassword` 🔓 | `?email` |
| DELETE | `ResetPassword` 🔓 | `?token&email&password&confirmPassword` |
| PUT | `ChangePassword` | `?oldPassword&password&confirmPassword` |

### User — `/User`
| Метод | Путь | Параметры |
|---|---|---|
| GET | `get-users` | `?userName&email&pageNumber&pageSize` |
| POST | `add-search-history` | `?text` |
| GET | `get-search-histories` | — |
| DELETE | `delete-search-history` | `?id` |
| DELETE | `delete-search-histories` | — |
| POST | `add-user-search-history` | `?userSearchId` |
| GET | `get-user-search-histories` | — |
| DELETE | `delete-user-search-history` | `?id` |
| DELETE | `delete-user-search-histories` | — |
| DELETE | `delete-user` 🛡 | `?userId` |

### UserProfile — `/UserProfile`
| Метод | Путь | Параметры |
|---|---|---|
| GET | `get-user-profile-by-id` | `?id` |
| GET | `get-is-follow-user-profile-by-id` | `?followingUserId` |
| GET | `get-my-profile` | — |
| PUT | `update-user-profile` | body `UpdateUserProfileDto` |
| GET | `get-post-favorites` | `?pageNumber&pageSize` |
| PUT | `update-user-image-profile` | multipart `imageFile` |
| DELETE | `delete-user-image-profile` | — |

### FollowingRelationShip — `/FollowingRelationShip`
| Метод | Путь | Параметры |
|---|---|---|
| GET | `get-subscribers` | `?userId` (только одобренные) |
| GET | `get-subscriptions` | `?userId` (только одобренные) |
| POST | `add-following-relation-ship` | `?followingUserId` (публичный → сразу, приватный → запрос) |
| DELETE | `delete-following-relation-ship` | `?followingUserId` |
| GET | `get-follow-requests` | `?pageNumber&pageSize` (входящие запросы) |
| POST | `accept-request` | `?requesterUserId` |
| POST | `decline-request` | `?requesterUserId` |
| DELETE | `cancel-request` | `?followingUserId` (отменить свой запрос) |

### Post — `/Post`
| Метод | Путь | Параметры |
|---|---|---|
| GET | `get-posts` | `?userId&title&content&pageNumber&pageSize` |
| GET | `get-reels` | `?pageNumber&pageSize` |
| GET | `get-post-by-id` | `?id` |
| GET | `get-my-posts` | — |
| GET | `get-following-post` | `?userId&pageNumber&pageSize` |
| POST | `add-post` | multipart `AddPostDto` (Title, Content, IsReel, **Images**) |
| DELETE | `delete-post` | `?id` (только автор) |
| POST | `like-post` | `?postId` (тумблер) |
| POST | `view-post` | `?postId` (уникально на юзера) |
| POST | `add-comment` | body `AddPostCommentDto` |
| DELETE | `delete-comment` | `?commentId` (только автор) |
| POST | `add-post-favorite` | body `AddPostFavoriteDto` (тумблер) |

### Story — `/Story`  (сторис живёт 24 часа)
| Метод | Путь | Параметры |
|---|---|---|
| GET | `get-stories` | — (лента подписок) |
| GET | `get-user-stories/{userId}` | route `userId` |
| GET | `get-my-stories` | — |
| POST | `LikeStory` | `?storyId` (тумблер) |
| GET | `GetStoryById` | `?id` |
| POST | `AddStories` | `?postId` + multipart `AddStoryDto` (Image, **Audience**: All/CloseFriends) |
| DELETE | `DeleteStory` | `?id` (только автор) |
| POST | `add-story-view` | `?storyId` (уникально на юзера) |
| POST | `reply` | `?storyId` + body `{ text }` → личное сообщение автору + уведомление |
| POST | `share-post` | `?postId` (репост поста в свою сторис) |

### Chat — `/Chat`
| Метод | Путь | Параметры |
|---|---|---|
| GET | `get-chats` | — (последнее сообщение + непрочитанные) |
| GET | `get-chat-by-id` | `?chatId` (помечает входящие прочитанными) |
| POST | `create-chat` | `?receiverUserId` (дедуп) |
| PUT | `send-message` | multipart `SendMessageDto` (ChatId, MessageText, File) + SignalR |
| DELETE | `delete-message` | `?massageId` (только отправитель; опечатка контракта сохранена) |
| DELETE | `delete-chat` | `?chatId` (только участник) |

### Location — `/Location`
| Метод | Путь | Параметры |
|---|---|---|
| GET | `get-Locations` | `?city&state&zipCode&country&pageNumber&pageSize` |
| GET | `get-Location-by-id` | `?id` |
| POST | `add-Location` | body `AddLocationDto` |
| PUT | `update-Location` | body `UpdateLocationDto` (с `locationId`) |
| DELETE | `delete-Location` | `?id` |

### Notification — `/Notification`
| Метод | Путь | Параметры |
|---|---|---|
| GET | `get-notifications` | `?pageNumber&pageSize` (группировка за 24ч) |
| GET | `get-unread-count` | — |
| PUT | `mark-as-read` | `?id` |
| PUT | `mark-all-as-read` | — |
| DELETE | `delete-notification` | `?id` |

### Settings — `/Settings`  (приватность)
| Метод | Путь | Параметры |
|---|---|---|
| GET | `get-privacy` | — |
| PUT | `update-privacy` | body `UpdatePrivacySettingsDto` (IsPrivate, ShowOnlineStatus, WhoCanMessage/Mention/ReplyStory) |

### Block — `/Block`
| Метод | Путь | Параметры |
|---|---|---|
| POST | `block-user` | `?userId` (взаимная отписка) |
| DELETE | `unblock-user` | `?userId` |
| GET | `get-blocked-users` | `?pageNumber&pageSize` |

### Hashtag — `/Hashtag`
| Метод | Путь | Параметры |
|---|---|---|
| GET | `search` | `?query&pageNumber&pageSize` |
| GET | `get-posts-by-tag` | `?tag&pageNumber&pageSize` (с фильтром блок/приват) |
| GET | `get-trending` | `?pageNumber&pageSize` (за 7 дней) |

### CloseFriend — `/CloseFriend`
| Метод | Путь | Параметры |
|---|---|---|
| POST | `add` | `?userId` |
| DELETE | `remove` | `?userId` |
| GET | `get-list` | `?pageNumber&pageSize` |

### GroupChat — `/GroupChat`  (роли Admin/Member; SignalR `/groupChatHub`)
| Метод | Путь | Параметры |
|---|---|---|
| POST | `create` | body `CreateGroupChatDto` (Name, MemberUserIds) |
| GET | `get-my-groups` | `?pageNumber&pageSize` |
| GET | `get-group-by-id` | `?groupId` (помечает прочитанным) |
| POST | `add-member` | `?groupId&userId` (только Admin группы) |
| DELETE | `remove-member` | `?groupId&userId` (только Admin группы) |
| POST | `promote-admin` | `?groupId&userId` (только Admin группы) |
| POST | `leave` | `?groupId` |
| PUT | `update-info` | `?groupId` + multipart (Name, Avatar) — только Admin группы |
| PUT | `send-message` | `?groupId` + multipart (MessageText, File, ReplyToMessageId) |
| DELETE | `delete-message` | `?messageId` (только отправитель) |

### Message — `/Message`  (реакции/forward/голосовые; личные и групповые)
| Метод | Путь | Параметры |
|---|---|---|
| POST | `react` | `?messageId&context&emoji` (тумблер/замена) |
| POST | `forward` | `?messageId&context&targetChatId&targetContext` |
| POST | `send-voice` | multipart `SendVoiceDto` (Context, ChatId, File, Duration, ReplyToMessageId) |

`context` — `Direct` (личный `Message`) или `Group` (`GroupMessage`).
`Chat/send-message` расширен необязательным `ReplyToMessageId`; реакции и цитата reply отдаются в DTO сообщений.

### Presence — `/Presence`  (онлайн-статусы; SignalR `/presenceHub`)
| Метод | Путь | Параметры |
|---|---|---|
| GET | `get-status` | `?userId` (`isOnline` + `lastSeen`) |
| POST | `get-statuses` | body `PresenceQueryDto` (`{ userIds }`) |

Presence взаимна: если у запрашивающего выключен `ShowOnlineStatus`, он не видит чужие статусы, а его — скрыт для всех.

### Admin — `/Admin`  🛡 (только роль Admin)
| Метод | Путь | Параметры |
|---|---|---|
| POST | `verify-user` | `?userId` (ставит `isVerified`) |
| DELETE | `unverify-user` | `?userId` |
| POST | `grant-admin` | `?userId` |
| DELETE | `revoke-admin` | `?userId` (защита от самолокаута) |

### Explore — `/Explore`  (рекомендации, content-based)
| Метод | Путь | Параметры |
|---|---|---|
| GET | `get-feed` | `?pageNumber&pageSize` (персональная лента по интересам) |
| GET | `get-popular` | `?pageNumber&pageSize` (cold start — популярное) |

## SignalR — реальное время
Все хабы требуют JWT. WebSocket не шлёт заголовок `Authorization`, поэтому токен передаётся
query-параметром `access_token`, например `ws://localhost:<port>/chatHub?access_token=<JWT>`.

| Хаб | Назначение | Ключевые события (сервер → клиент) |
|---|---|---|
| `/chatHub` | Личные чаты, typing | `ReceiveMessage`, `ReceiveReaction`, `UserTyping` |
| `/groupChatHub` | Групповые чаты, typing | `ReceiveMessage`, `ReceiveReaction`, `GroupTyping` |
| `/notificationHub` | Уведомления (live «звоночек») | `ReceiveNotification` |
| `/presenceHub` | Онлайн-статусы | `ReceivePresence` |

- Личный `PUT /Chat/send-message` / голосовое → `ReceiveMessage(GetMessageDto)` обоим участникам.
- Групповое сообщение (обычное и системное) → `ReceiveMessage` всем участникам группы.
- Реакция (`POST /Message/react`) → `ReceiveReaction` в соответствующий хаб.
- Typing: клиент вызывает `ChatHub.Typing(chatId, kind)` / `GroupChatHub.Typing(groupChatId, kind)`
  (`kind ∈ {text, voice}`) — эфемерно, в БД не пишется.
- Presence-изменения рассылаются адресно собеседникам по личным чатам и участникам общих групп.

## Новые фичи (Phase 11–21) — кратко
- **Уведомления** (§2): типы Like/Follow/Comment/Mention/CommentReply/CommentLike/FollowRequest/
  FollowRequestAccepted/StoryReply/PostShared; группировка за 24ч; live через `/notificationHub`.
- **Приватность и блокировки** (§6): приватные аккаунты + follow requests, блокировка (взаимная
  отписка), настройки `WhoCanMessage/Mention/ReplyStory`; сквозная фильтрация выдач через `AccessGuard`.
- **Хэштеги и упоминания** (§3–4): парсинг `#tag`/`@username`, тренды, `MentionedUsers` в DTO.
- **Комментарии** (§5): ответы (2 уровня) + лайки комментов.
- **Групповые чаты** (§7): роли Admin/Member, системные сообщения, непрочитанные на участника.
- **Сообщения** (§8): реакции (полиморфные), reply-цитата, forward (копия), голосовые (`wwwroot/voice`).
- **Сторис** (§9): close friends, ответы в директ, репост поста.
- **Presence/typing** (§1): онлайн по живым SignalR-соединениям, взаимная приватность статуса.
- **Верификация** (§10): `isVerified` во всех author-DTO, админ-эндпоинты.
- **2FA** (§11): TOTP (RFC 6238) + email-код + backup-коды.
- **Explore** (§12): content-based рекомендации без ML (профиль интересов на лету).

## Рабочий процесс
Проект ведётся сессиями: `/start` продолжает работу по роадмапу, `/stop` фиксирует изменения.
Логи сессий — в [`.claude/sessions/`](./.claude/sessions/).
