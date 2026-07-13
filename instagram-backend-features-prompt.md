# Промпт: Расширение бэкенда Instagram-клона — новые фичи

> Это **дополнение** к уже готовому бэкенду (ASP.NET Core 8 + EF Core + PostgreSQL + Identity + JWT + SignalR), собранному по основному ТЗ (`instagram-backend-prompt.md`).
> Основа уже включает: аккаунты, профили, посты, reels, сторис, лайки/просмотры/комменты, подписки, чат 1:1, поиск, локации, базовый presence и typing.
> Задача этого промпта — **добавить новые модули поверх основы, не ломая существующий контракт**.

---

Ты — senior .NET backend разработчик. Расширь существующий бэкенд Instagram-клона (ASP.NET Core 8, EF Core 8 + PostgreSQL, ASP.NET Identity, JWT, AutoMapper, FluentValidation, SignalR, Swagger) перечисленными ниже фичами. Сохраняй архитектуру и стиль основы: слои Domain / Infrastructure / WebApi, единый формат ответа `Response<T>` и `PagedResponse<T>`, ID текущего юзера — из JWT claims, файлы — в `wwwroot/images` (в БД только имена). Все новые эндпоинты по умолчанию требуют авторизации. Создай миграции и обнови сид.

---

## 0. Новые/изменённые сущности (сводка)

**Новые сущности:**
`Notification`, `Hashtag`, `PostHashtag`, `Mention`, `CommentLike`, `Block`, `PrivacySettings`, `GroupChat`, `GroupChatMember`, `GroupMessage`, `MessageReaction`, `CloseFriend`, `StoryReply`, `UserInterest` (агрегат интересов для Explore), `BackupCode` (для 2FA).

**Изменения существующих сущностей:**
- **User:** `IsPrivate (bool, default false)`, `IsVerified (bool, default false)`, `TwoFactorEnabled (bool)`, `TwoFactorSecret (string?, для TOTP)`, `LastSeen`, `ShowOnlineStatus` (последние два уже есть из основы).
- **FollowingRelationShip:** `Status (enum: Pending=0, Accepted=1)` — для follow requests на приватные аккаунты.
- **PostComment:** `ParentCommentId (int?, nullable)` — для ответов (максимум 2 уровня).
- **Post / Story / PostComment / Message:** участвуют в упоминаниях — упоминания хранятся в отдельной `Mention` с полиморфной ссылкой (тип объекта + id).
- **Story:** `Audience (enum: All=0, CloseFriends=1)`, `SharedPostId (int?, nullable — репост поста в сторис)`.
- **Message (личный чат):** `ReplyToMessageId (int?)`, `IsForwarded (bool)`, `MessageType (enum: Text=0, Image=1, File=2, Voice=3)`, `Duration (int?, сек — для voice)`, `Waveform (string?, JSON-массив амплитуд — для voice)`.

---

## 1. Real-time статусы (усиление существующего)

### Онлайн-статус — взаимная приватность
- Поле `ShowOnlineStatus` уже есть. Логика **взаимная**: если юзер A выключил показ своего статуса, то A **не видит** статусы других (везде отдавать `offline`/`lastSeen=null`), и его статус тоже скрыт для всех. Реализовать проверку в сервисе presence: перед выдачей статуса любого юзера проверять `ShowOnlineStatus` **запрашивающего** — если false, вернуть скрытый статус.
- Форматирование «был(а) в сети»: сервер отдаёт `isOnline` + `lastSeen (DateTime?)`; человекочитаемую строку («только что», «N минут назад», «вчера», дата) формирует клиент.

### Typing — с именами, группы, голосовые
- В личном чате: событие `UserTyping{chatId, userId, userName, kind}`, где `kind ∈ {text, voice}`.
- В групповом чате (см. §7): если печатает один — `UserTyping{... userName}`; если несколько — клиенту уходит список печатающих, отображение «X и ещё N печатают…».
- При записи голосового слать `kind=voice` → «записывает голосовое…».
- Событие эфемерное, в БД не сохраняется, авто-сброс ~3 сек на клиенте.

### Delivered / Read — без изменений
Оставить как в основе (галочки доставлено/прочитано, без настройки скрытия).

---

## 2. Уведомления (Notifications)

**Сущность `Notification`:** `Id, RecipientUserId (FK), ActorUserId (FK — кто инициировал), Type (enum), EntityType (enum: Post/Comment/Story/User), EntityId (int?), IsRead (bool), CreatedAt`.

**Типы (`NotificationType`):** `Like, Follow, Comment, Mention, CommentReply, CommentLike, FollowRequest, FollowRequestAccepted, StoryReply, PostShared`.

**Доставка:** сохранять в БД **и** пушить real-time через SignalR-хаб `NotificationHub` (событие `ReceiveNotification`), чтобы «звоночек» и счётчик обновлялись live.

**Группировка:** одинаковые уведомления на один объект за короткий период объединять в одно отображаемое: «user1, user2 и ещё N лайкнули ваше фото». Хранить можно отдельные записи, но в выдаче группировать по `(Type, EntityType, EntityId)` за окно времени, отдавая `actors` (первые 2–3 + суммарный счётчик).

**Эндпоинты:**
- `GET /Notification/get-notifications?PageNumber&PageSize` — список (сгруппированный), с пагинацией.
- `GET /Notification/get-unread-count` — счётчик непрочитанных.
- `PUT /Notification/mark-as-read?id` — отметить одно.
- `PUT /Notification/mark-all-as-read` — отметить все.
- `DELETE /Notification/delete-notification?id`

Уведомления создаются автоматически из соответствующих действий (лайк, коммент, подписка, упоминание, ответ, лайк коммента, запрос на подписку и его принятие, ответ на сторис, репост поста). Не создавать уведомление на собственное действие (лайк своего поста и т.п.).

---

## 3. Хэштеги (Hashtags)

**Сущности:** `Hashtag { Id, Tag (unique, lowercase), PostsCount, CreatedAt }`, `PostHashtag { Id, PostId (FK), HashtagId (FK) }` (many-to-many).

**Логика:** при создании/редактировании поста парсить `#tag` из Title/Content, нормализовать (нижний регистр, без символа `#`), создавать отсутствующие хэштеги, связывать с постом, инкрементить `PostsCount`. При удалении поста — декремент.

**Эндпоинты:**
- `GET /Hashtag/search?query&PageNumber&PageSize` — автодополнение/поиск тегов по префиксу, сортировка по популярности.
- `GET /Hashtag/get-posts-by-tag?tag&PageNumber&PageSize` — лента постов по хэштегу (свежие + топ).
- `GET /Hashtag/get-trending?PageNumber&PageSize` — популярные теги за период.

---

## 4. Упоминания (@Mentions)

**Сущность `Mention`:** `Id, MentionedUserId (FK), AuthorUserId (FK), EntityType (enum: Post/Comment/StoryReply), EntityId (int), CreatedAt`.

**Логика:** при сохранении поста, комментария и ответа на сторис парсить `@username`, находить существующих юзеров, создавать `Mention` и уведомление типа `Mention`. Бэк возвращает список упомянутых юзеров (id + username) в DTO объекта, чтобы фронт сделал кликабельные ссылки на профиль.

Учитывать настройку приватности «кто может упоминать» (см. §6): если упоминающий не входит в разрешённую аудиторию адресата — упоминание не создаётся (или создаётся без уведомления, по флагу настройки).

---

## 5. Комментарии: ответы (2 уровня) + лайки

### Ответы на комментарии
- `PostComment.ParentCommentId (int?)`. Максимум **2 уровня**: комментарий верхнего уровня и ответы под ним. Ответ на ответ прикрепляется к тому же родителю верхнего уровня (не создаём 3-й уровень).
- При ответе автоматически подставлять `@username` того, кому отвечаешь, в начало текста + создавать `Mention` + уведомление `CommentReply` автору исходного коммента.
- Эндпоинты:
  - `POST /Post/add-comment` (существует) — расширить: принимать необязательный `parentCommentId`.
  - `GET /Post/get-comment-replies?commentId&PageNumber&PageSize` — ответы под комментом.
  - В выдаче комментов верхнего уровня возвращать `repliesCount`.

### Лайки комментариев
- Сущность `CommentLike { Id, CommentId (FK), UserId (FK), CreatedAt }`.
- `POST /Post/like-comment?commentId` — тумблер лайка коммента; уведомление `CommentLike` автору.
- В DTO коммента: `likesCount`, `isLiked` (для текущего юзера).

---

## 6. Приватность: приватные аккаунты, follow requests, блокировка, настройки

### Приватные аккаунты
- `User.IsPrivate`. При включении — **текущие подписчики остаются** (их связи уже `Accepted`); новые подписки идут через запрос.
- Чужому пользователю у приватного аккаунта видны только: аватар, имя, bio, счётчики (посты/подписчики/подписки). Посты, сторис, списки подписок/подписчиков — скрыты, пока он не одобренный подписчик.

### Follow requests
- `FollowingRelationShip.Status (Pending/Accepted)`. Подписка на публичный аккаунт → сразу `Accepted`. На приватный → `Pending` + уведомление `FollowRequest` владельцу.
- Эндпоинты:
  - `GET /FollowingRelationShip/get-follow-requests?PageNumber&PageSize` — входящие pending-запросы.
  - `POST /FollowingRelationShip/accept-request?requesterUserId` — принять (→ Accepted + уведомление `FollowRequestAccepted` запросившему).
  - `POST /FollowingRelationShip/decline-request?requesterUserId` — отклонить (удалить запрос).
  - `DELETE /FollowingRelationShip/cancel-request?followingUserId` — отменить свой исходящий запрос.
- Существующие get-subscribers/get-subscriptions учитывают только `Accepted`.

### Блокировка
- Сущность `Block { Id, BlockerUserId (FK), BlockedUserId (FK), CreatedAt }`.
- При блокировке: **обе стороны отписываются друг от друга** (удаляются обе связи в любом статусе); мой профиль/посты/сторис/директ становятся невидимы заблокированному и наоборот; писать друг другу нельзя. **Старые лайки и комментарии остаются** (не удаляются).
- Заблокированный не может: видеть профиль/контент, подписаться, писать, упоминать, отвечать на сторис.
- Эндпоинты: `POST /Block/block-user?userId`, `DELETE /Block/unblock-user?userId`, `GET /Block/get-blocked-users?PageNumber&PageSize`.
- Во всех выдачах (лента, поиск, комменты-визибилити, explore, чат-создание) фильтровать пользователей, с которыми есть блок в любую сторону.

### Настройки приватности
- Сущность `PrivacySettings { Id, UserId (FK 1:1), IsPrivate, ShowOnlineStatus, WhoCanMessage (enum: Everyone/Followers/Nobody), WhoCanMention (enum: Everyone/Followers/Nobody), WhoCanReplyStory (enum: Everyone/Followers/CloseFriends/Nobody) }`. (IsPrivate/ShowOnlineStatus дублируют флаги User — держать источником истины `PrivacySettings`, синхронизировать с User либо читать из настроек.)
- Эндпоинты: `GET /Settings/get-privacy`, `PUT /Settings/update-privacy` (body с перечисленными полями).
- Эти настройки применяются в §1 (онлайн), §4 (упоминания), §7 (директ), §8 (ответы на сторис).

---

## 7. Групповые чаты (отдельная ветка от личных 1:1)

Личные чаты (`Chat`/`Message` из основы) **не трогаем** — группы это **отдельные сущности**.

**Сущности:**
- `GroupChat { Id, Name, Avatar (nullable), CreatorUserId (FK), CreatedAt }`.
- `GroupChatMember { Id, GroupChatId (FK), UserId (FK), Role (enum: Admin/Member), JoinedAt }`.
- `GroupMessage { Id, GroupChatId (FK), SenderUserId (FK?), MessageText (nullable), FileName (nullable), MessageType (Text/Image/File/Voice/System), Duration (int?), Waveform (string?), ReplyToMessageId (int?), IsForwarded (bool), CreatedAt }`.

**Правила:**
- Группа остаётся группой **даже если в ней остался 1 участник**.
- Роли: **Admin** (создатель — стартовый админ) может добавлять/удалять участников, менять название/аватар и **назначать других участников админами** (`POST .../promote`). Обычный **Member** — только писать и выйти из группы.
- **Системные сообщения** (`MessageType=System`, `SenderUserId=null`): «X создал группу», «X добавил Y», «X удалил Y», «X вышел», «X сменил название», «X назначен админом». Показываются в ленте сообщений как служебные.

**Эндпоинты:**
- `POST /GroupChat/create` — body `{ name, memberUserIds[] }`.
- `GET /GroupChat/get-my-groups?PageNumber&PageSize`.
- `GET /GroupChat/get-group-by-id?groupId` — инфо + участники + сообщения (пометить прочитанными).
- `POST /GroupChat/add-member?groupId&userId` — только Admin.
- `DELETE /GroupChat/remove-member?groupId&userId` — только Admin.
- `POST /GroupChat/promote-admin?groupId&userId` — только Admin.
- `POST /GroupChat/leave?groupId` — выйти самому.
- `PUT /GroupChat/update-info?groupId` — `multipart/form-data`: Name, Avatar (только Admin).
- `PUT /GroupChat/send-message?groupId` — `multipart/form-data`: MessageText, File, ReplyToMessageId — рассылка через SignalR всем участникам.
- `DELETE /GroupChat/delete-message?messageId` — автор сообщения или админ.
- Реакции, reply, forward, voice (§ ниже) работают и в группах, и в личных чатах.

---

## 8. Сообщения: реакции, reply, forward, голосовые (для личных и групповых)

### Реакции
- Сущность `MessageReaction { Id, MessageId (FK), MessageContext (enum: Direct/Group), UserId (FK), Emoji (string), CreatedAt }`.
- Как в Instagram: быстрый набор эмодзи (❤️ 😂 😮 😢 👍 🔥 и т.п.) + возможность выбрать любой эмодзи. **Один юзер = одна реакция** на сообщение: повторная с тем же эмодзи снимает, с другим — заменяет.
- Эндпоинт `POST /Message/react?messageId&context&emoji` (тумблер/замена). Пуш real-time участникам.

### Reply
- Поле `ReplyToMessageId` в Message/GroupMessage. При ответе показывается цитата исходного (текст/превью). Отдавать в DTO краткую инфо об исходном сообщении.

### Forward
- Пересылка = **копия** содержимого (текст/файл) в целевой чат с флагом `IsForwarded=true` и пометкой «Переслано». Ссылку на оригинал не хранить.
- Эндпоинт `POST /Message/forward?messageId&context&targetChatId&targetContext`.

### Голосовые сообщения
- `MessageType=Voice`. Хранить: аудиофайл (в `wwwroot/images` или отдельной папке `wwwroot/voice`), `Duration` (сек), `Waveform` — JSON-массив нормализованных амплитуд для отрисовки волны (сгенерировать на сервере из аудио, напр. через NAudio/FFmpeg-обёртку; если генерация недоступна — сохранить равномерный плейсхолдер-массив, но предусмотреть поле).
- В связке с typing (§1): при записи слать `kind=voice` → «записывает голосовое…».
- Отправка через тот же send-message эндпоинт с типом voice (или отдельный `POST .../send-voice`).

---

## 9. Сторис: close friends, ответы, репост поста

### Close friends
- Сущность `CloseFriend { Id, UserId (FK), FriendUserId (FK), CreatedAt }`. В близкие можно добавлять в том числе своих подписчиков (как в Instagram — любого пользователя).
- При публикации сторис — параметр `audience (All/CloseFriends)`. `CloseFriends`-сторис видны только тем, кто в списке близких автора.
- Эндпоинты: `POST /CloseFriend/add?userId`, `DELETE /CloseFriend/remove?userId`, `GET /CloseFriend/get-list?PageNumber&PageSize`.
- В существующей выдаче сторис (`get-stories`, `get-user-stories`) фильтровать close-friends-сторис по членству зрителя в списке близких автора.

### Ответы на сторис
- Ответ на **активную** чужую сторис уходит в **директ** автора: создаётся личное сообщение (или новый чат 1:1, если его нет) с привязкой к сторис (превью) + текст. Сущность `StoryReply { Id, StoryId (FK), FromUserId (FK), MessageId (FK), CreatedAt }` (связывает ответ с созданным сообщением). Уведомление `StoryReply` автору.
- Учитывать настройку «кто может отвечать на сторис» (§6: Everyone/Followers/CloseFriends/Nobody).
- Эндпоинт `POST /Story/reply?storyId` — body `{ text }`.

### Репост поста в сторис
- Чужой **публичный** пост можно расшарить в свою сторис: `Story.SharedPostId` ссылается на оригинал; сторис показывает превью поста + ведёт к нему. Посты из приватных аккаунтов репостить **нельзя**.
- Уведомление автору оригинала: `PostShared`.
- Эндпоинт `POST /Story/share-post?postId` (создаёт сторис с `SharedPostId`).

---

## 10. Верификация (Admin)

- `User.IsVerified`. Роль **Admin** уже существует в основе (сид создаёт роли Admin/User; первый админ — в сиде). Действующий Admin может выдавать роль Admin другим (эндпоинт ниже).
- Отдельный **`AdminController`**, все методы `[Authorize(Roles="Admin")]`. Бэкенд не рисует админ-панель — предоставляет защищённые эндпоинты (используются через Swagger/Postman/будущий админ-фронт).
- Эндпоинты:
  - `POST /Admin/verify-user?userId` — поставить синюю галочку.
  - `DELETE /Admin/unverify-user?userId` — снять.
  - `POST /Admin/grant-admin?userId` — выдать роль Admin.
  - `DELETE /Admin/revoke-admin?userId` — снять роль Admin.
  - (Уже есть в основе: `DELETE /User/delete-user` для Admin.)
- Поле `isVerified` возвращать во всех DTO пользователя/автора (профиль, автор поста/коммента/сторис) для отрисовки галочки.

---

## 11. Двухфакторная аутентификация (TOTP + email-код + резервные коды)

- `User.TwoFactorEnabled`, `User.TwoFactorSecret` (для TOTP).
- **TOTP** (основной): при включении генерируется секрет, отдаётся `otpauth://`-URI + QR (строка для генерации QR на клиенте). Совместимо с Google Authenticator/Authy. Использовать встроенные возможности ASP.NET Identity / библиотеку типа `Otp.NET`.
- **Email-код** (резервный): 6-значный код на почту, срок жизни ~5–10 мин.
- **Резервные коды:** сущность `BackupCode { Id, UserId (FK), CodeHash, IsUsed (bool) }` — генерировать пачку одноразовых кодов при включении 2FA.
- **Флоу логина:** при `TwoFactorEnabled` эндпоинт `/Account/login` возвращает не токен, а промежуточный статус «нужен второй фактор» + временный `twoFactorToken`. Затем `POST /Account/login-2fa` с `{ twoFactorToken, code, method (Totp/Email/Backup) }` → выдаёт JWT.
- Эндпоинты управления:
  - `POST /Account/enable-2fa` → возвращает секрет/QR + резервные коды.
  - `POST /Account/confirm-2fa?code` — подтвердить включение первым валидным кодом.
  - `POST /Account/disable-2fa?code`
  - `POST /Account/send-2fa-email` — выслать email-код (в рамках login-флоу).
  - `POST /Account/regenerate-backup-codes`

---

## 12. Explore / рекомендации (content-based, «как в инсте» без ML)

Настоящий рекомендательный движок на основе поведения пользователя (без нейросети, но реально работающий: «лайкал такое → показываю похожее»).

### Профиль интересов
- Сущность `UserInterest` (агрегат) или расчёт на лету: для текущего юзера собираем сигналы взаимодействия:
  - веса действий: **сохранение (favorite) > комментарий > лайк > просмотр**;
  - **затухание по времени**: свежие действия весят больше старых (экспоненциальное/линейное затухание).
- Из этих действий агрегируем: **любимые хэштеги** (по постам, с которыми взаимодействовал) и **любимых/похожих авторов**.

### Скоринг кандидатов
Каждому посту-кандидату присваивается балл:
`score = w1·совпадение_хэштегов_с_интересами + w2·близость_автора(взаимодействия/похожесть) + w3·популярность(лайки+комменты+просмотры за период) + w4·свежесть`.

### Фильтры (исключить из выдачи)
- собственные посты;
- уже просмотренные (по `PostView`);
- посты заблокированных / заблокировавших;
- посты приватных аккаунтов, на которые нет `Accepted`-подписки;
- посты авторов, на которых уже подписан (Explore — про **открытие нового**).

### Разнообразие
- Не более N постов подряд от одного автора; перемешивать топ по score с элементом свежести.

**Эндпоинт:** `GET /Explore/get-feed?PageNumber&PageSize` — персональная лента рекомендаций. Дополнительно `GET /Explore/get-popular?PageNumber&PageSize` — фолбэк по чистой популярности для новых юзеров без истории (cold start).

---

## 13. Требования к качеству (в дополнение к основе)

1. Все новые эндпоинты — в едином формате `Response<T>` / `PagedResponse<T>`, с обработкой ошибок через существующий middleware.
2. Везде проверять блокировки и приватность (не отдавать контент/статусы тем, кому не положено).
3. Не создавать уведомления/mention на собственные действия.
4. Авторизация владельца/роли: удалять/редактировать может только владелец; админ-действия — только роль Admin; групп-действия — по роли в группе.
5. Пагинация на всех списках.
6. Real-time (SignalR) для: уведомлений, сообщений (личных и групповых), реакций, typing, presence.
7. Валидация (FluentValidation) для новых DTO; безопасная загрузка файлов (voice/аватар группы) с проверкой типа и размера.
8. Миграции EF Core для всех новых сущностей и изменённых полей; обновить сид (роли, тестовые данные, пример приватного аккаунта, пример группы).
9. Обновить Swagger: новые контроллеры сгруппированы по тегам, Bearer-авторизация, описания.
10. Обратная совместимость: существующие эндпоинты основы не менять по контракту (только расширять необязательными полями, как `parentCommentId`).

## Что отдать в результате

Все новые сущности + конфигурации EF, DTO, AutoMapper-профили, валидаторы, сервисы с бизнес-логикой, контроллеры, SignalR-хабы (Notification, расширенный Chat/Group), миграции, обновлённый сид, обновлённый README (описание новых эндпоинтов и фич). Код должен компилироваться и работать вместе с основой без правок существующего контракта.
