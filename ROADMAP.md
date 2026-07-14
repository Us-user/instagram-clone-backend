# ROADMAP — Instagram Clone Backend (ASP.NET Core 8 + PostgreSQL)

Источники истины:
- **База (готово):** [`instagram-backend-prompt.md`](./instagram-backend-prompt.md)
- **Новые фичи (текущая работа):** [`instagram-backend-features-prompt.md`](./instagram-backend-features-prompt.md)

Проект ведётся сессиями: `/start` продолжает работу, `/stop` фиксирует изменения.
Логи сессий: [`.claude/sessions/`](./.claude/sessions/).

## Легенда статусов
- `[ ]` — не начато
- `[~]` — в работе
- `[x]` — готово

## 📍 Текущий статус
- **Активная фаза:** Phase 18 — Real-time статусы: presence + typing (§1) — **не начата**. Phase 17 (сторис: close friends/ответы/репост) завершена.
- **Последняя сессия:** 2026-07-14 (21) — реализована Phase 17 (§9): enum `StoryAudience {All, CloseFriends}`; сущности `CloseFriend` (направленная, unique-пара, две FK-каскад), `StoryReply { StoryId, FromUserId, MessageId }` (все FK каскадом); расширен `Story` (`Audience`, `SharedPostId` — FK Posts `SET NULL`, отдельно от `PostId`); новый `CloseFriendService`/`ICloseFriendService` + `CloseFriendController` (`/CloseFriend/add|remove|get-list`, идемпотентные, блок-проверка при add); `StoryService` расширен `ReplyAsync` (ответ в директ: `GetOrCreateChatAsync` + Direct-сообщение + `StoryReply` через nav, уведомление `StoryReply`, real-time `IChatNotifier`, учёт `WhoCanReplyStory`, только активная чужая сторис, блок-проверка, mention-проводка `StoryReply`) и `SharePostAsync` (репост публичного поста → сторис с `SharedPostId`, уведомление `PostShared`, блок/приват-проверка); close-friends-фильтр в `get-stories`/`get-user-stories`/`GetStoryById`; `AddStoryDto.Audience` (необязательно), `GetStoryDto` + `Audience`/`SharedPostId`/`SharedPost`-превью; валидатор `StoryReplyRequestDto`; миграция `AddStoryCloseFriendsAndReplies` + сид (сторис alice All/CloseFriends, close-friend alice→bob, ответ bob, репост поста alice в сторис bob, уведомления).
- **Следующий шаг:** начать Phase 18 — presence (взаимная логика `ShowOnlineStatus`: `isOnline`+`lastSeen`) и typing (личный чат `UserTyping{chatId,userId,userName,kind}`, `kind∈{text,voice}`; в группах — список печатающих; эфемерно, без записи в БД).
- **Состояние сборки:** 🟢 зелёная (0 предупреждений; база 94 операции эндпоинтов: 89 + 5 Phase 17: 3 `/CloseFriend/*` + `/Story/reply` + `/Story/share-post`). Проверено: сборка, offline-скрипт миграции (2 колонки в `Stories` + `Audience` default 0, таблицы `CloseFriends`/`StoryReplies` с каскадами, unique `(UserId, FriendUserId)`, `FK_Stories_Posts_SharedPostId ON DELETE SET NULL` — SQL корректен), boot приложения (БД недоступна → ошибка миграции поймана, хост поднялся на :5030) + регистрация всех 5 новых эндпоинтов в Swagger (`/Story/reply` — body `{ text }`). Живой smoke-тест на PostgreSQL в эту сессию не гонялся (нет доступа к `DATABASE_URL`), запускается при деплое.

## ⚠️ Инварианты для всех новых фаз (не нарушать)
1. **Обратная совместимость.** Существующие эндпоинты базы по контракту не менять — только расширять **необязательными** полями (напр. `parentCommentId`). Сохранять оригинальные опечатки контракта (`massageId`).
2. Единый формат ответа `Response<T>` / `PagedResponse<T>`, ошибки — через существующий middleware.
3. **ID текущего юзера — из JWT claims**, не из параметров. Новые эндпоинты авторизованы по умолчанию.
4. **Приватность и блокировки проверять везде** (лента, поиск, комменты, explore, чат, сторис, presence) — не отдавать контент/статусы тем, кому не положено.
5. **Не создавать уведомления/mention на собственные действия.**
6. Авторизация владельца/роли: править/удалять — только владелец; админ-действия — роль `Admin`; групп-действия — по роли в группе.
7. Пагинация на всех списках; FluentValidation на новых DTO; безопасная загрузка файлов (voice, аватар группы).
8. Каждая фаза: миграция EF Core + обновление сида (по необходимости) + держать сборку зелёной (`dotnet build`).
9. Swagger: новые контроллеры сгруппированы по тегам, Bearer-авторизация, описания.
10. Поле `isVerified` отдавать во всех DTO пользователя/автора (профиль, автор поста/коммента/сторис).

---

## ✅ База — Phase 0–10 (ЗАВЕРШЕНО)
> Полный бэкенд по `instagram-backend-prompt.md`. 57 эндпоинтов, smoke-тест на живом PostgreSQL (Render) 12/12 PASS. Детали — в логах сессий 01–13.

- [x] **Phase 0** — Фундамент: solution, слои Domain/Infrastructure/WebApi, NuGet, `Program.cs`, `wwwroot/images`.
- [x] **Phase 1** — Доменная модель: Entities, Enums, DTO, `Response<T>`/`PagedResponse<T>`.
- [x] **Phase 2** — Слой данных: `DataContext : IdentityDbContext`, Fluent API, миграция, Seed, авто-применение.
- [x] **Phase 3** — Инфраструктура: JWT, middleware ошибок, FileService, AutoMapper, FluentValidation, Swagger, CurrentUser из claims, CORS.
- [x] **Phase 4** — Account: register/login/forgot/reset/change-password, валидаторы, авто-создание UserProfile.
- [x] **Phase 5** — Пользователи/профили/подписки: get-users, история поиска, UserProfile, FollowingRelationShip.
- [x] **Phase 6** — Посты: CRUD, ленты, лайки/комменты/просмотры/избранное.
- [x] **Phase 7** — Сторис: жизнь 24ч, лайки/просмотры/вьюеры.
- [x] **Phase 8** — Чат + SignalR `ChatHub`: чаты, сообщения real-time.
- [x] **Phase 9** — Локации и поиск.
- [x] **Phase 10** — Качество: аудит авторизации/пагинации/валидации/ошибок, README, smoke-тест, зелёная сборка.

---

# 🚀 Новые фичи (`instagram-backend-features-prompt.md`)

> Порядок фаз выстроен по зависимостям: сначала кросс-срезовые сервисы (уведомления, приватность/блокировки), затем фичи, которые на них опираются. Каждая фаза = сущности + сервис + контроллер + миграция + проводка real-time/уведомлений, где нужно.

## Phase 11 — Уведомления (Notifications + NotificationHub) · §2
> Кросс-срезовый сервис: строим первым, чтобы последующие фазы сразу подключали свои уведомления.

- [x] Сущность `Notification { Id, RecipientUserId, ActorUserId, Type, EntityType, EntityId?, IsRead, CreatedAt }`
- [x] Enum `NotificationType { Like, Follow, Comment, Mention, CommentReply, CommentLike, FollowRequest, FollowRequestAccepted, StoryReply, PostShared }`
- [x] Enum `NotificationEntityType { Post, Comment, Story, User }`
- [x] EF-конфигурация + индексы, миграция (`AddNotifications`; заодно добавлено `User.IsVerified` для честного `isVerified` в actor-DTO)
- [x] `INotificationService` + `NotificationService`: create (с проверкой «не себе»), выдача с **группировкой** по `(Type, EntityType, EntityId)` за окно 24ч (actors: первые 3 + счётчик), unread-count, mark-as-read/all, delete
- [x] `NotificationHub` (SignalR) + `INotificationNotifier`/`NotificationNotifier` → событие `ReceiveNotification` (live «звоночек» и счётчик); токен для хаба из `access_token`
- [x] Эндпоинты: `GET /Notification/get-notifications`, `GET /Notification/get-unread-count`, `PUT /Notification/mark-as-read?id`, `PUT /Notification/mark-all-as-read`, `DELETE /Notification/delete-notification?id`
- [x] Проводка существующих действий: уведомления `Like` (лайк поста), `Comment`, `Follow` — из `PostService`/`FollowingRelationShipService`
- [x] Регистрация в DI (`INotificationService`, `INotificationNotifier`). AutoMapper-профиль и валидаторы не потребовались: DTO собирается вручную (кастомная группировка, как в `ChatService`), входные параметры — только query

## Phase 12 — Приватность: приватные аккаунты, follow requests, блокировка, настройки · §6 ✅
> Кросс-срезовый фундамент для упоминаний, чата, сторис, explore.

- [x] `User.IsPrivate` + сущность `PrivacySettings { Id, UserId(1:1), IsPrivate, ShowOnlineStatus, WhoCanMessage, WhoCanMention, WhoCanReplyStory }` (источник истины — `PrivacySettings`, ленивое создание, синхронизация `User.IsPrivate` при update)
- [x] Enum'ы `WhoCanMessage/WhoCanMention {Everyone, Followers, Nobody}`, `WhoCanReplyStory {Everyone, Followers, CloseFriends, Nobody}` (+`FollowStatus {Pending=0, Accepted=1}`)
- [x] `FollowingRelationShip.Status (enum: Pending=0, Accepted=1)`; подписка на публичный → `Accepted` (+`Follow`), на приватный → `Pending` + уведомление `FollowRequest`. Без model-level default (иначе EF подменял бы явный Pending на store-default); backfill Accepted в миграции
- [x] Follow requests: `GET /FollowingRelationShip/get-follow-requests`, `POST /accept-request?requesterUserId` (+`FollowRequestAccepted`), `POST /decline-request?requesterUserId`, `DELETE /cancel-request?followingUserId`
- [x] get-subscribers/get-subscriptions учитывают только `Accepted` (+ скрыты у приватного чужого без одобренной подписки)
- [x] Сущность `Block { Id, BlockerUserId, BlockedUserId, CreatedAt }` + `BlockService`; при блокировке — взаимная отписка (обе связи, любой статус), старые лайки/комменты остаются
- [x] Эндпоинты: `POST /Block/block-user?userId`, `DELETE /Block/unblock-user?userId`, `GET /Block/get-blocked-users`
- [x] Приватный профиль чужому: видны только аватар/имя/bio/счётчики (+`isPrivate`/`isRequested` в DTO); посты/сторис/списки скрыты без `Accepted`-подписки; профиль скрыт при блокировке
- [x] Настройки: `GET /Settings/get-privacy`, `PUT /Settings/update-privacy`
- [x] **Фильтрация блокировок/приватности** в существующих выдачах через `AccessGuard`: лента (`get-posts`/`get-reels`/`get-following-post`/`get-post-by-id`), поиск (`get-users`), чат (create + send-message). Список комментов ещё не отдаётся отдельным эндпоинтом (только счётчик) — фильтр применится вместе с `get-comment-replies` в Phase 14
- [x] Миграция `AddPrivacyAndBlocks` + сид: приватный аккаунт `diana`, pending-запрос `carol→diana`, block `bob→carol`

## Phase 13 — Хэштеги + Упоминания · §3, §4 ✅
- [x] Сущности `Hashtag { Id, Tag(unique,lowercase), PostsCount, CreatedAt }`, `PostHashtag { Id, PostId, HashtagId }` (M:N)
- [x] Сущность `Mention { Id, MentionedUserId, AuthorUserId, EntityType(enum `MentionEntityType`: Post/Comment/StoryReply), EntityId, CreatedAt }` (пара `(MentionedUserId, EntityType, EntityId)` уникальна)
- [x] `HashtagService`: парсинг `#tag` из Title/Content при add поста → нормализация (lowercase), upsert, инкремент `PostsCount`; декремент + снятие связей при удалении поста. (Edit-эндпоинта в базе нет — правка появится вместе с ним)
- [x] `MentionService`: парсинг `@username` при сохранении поста/коммента → `Mention` + уведомление `Mention`; блок-проверка + учёт `WhoCanMention` (Everyone/Followers/Nobody); собственные упоминания игнорируются. (StoryReply-проводка — в Phase 17)
- [x] Эндпоинты Hashtag: `GET /Hashtag/search`, `GET /Hashtag/get-posts-by-tag?tag` (с `VisibleTo`-фильтром блок/приват), `GET /Hashtag/get-trending` (за 7 дней)
- [x] В DTO объектов отдавать список упомянутых юзеров (id + username) — `MentionedUsers` в `GetPostDto`/`GetPostCommentDto`, батч-заполнение `MentionEnrichment` во всех выдачах постов (ленты/по тегу/избранное/один пост) и в add-comment
- [x] Миграция `AddHashtagsAndMentions`; сид: хэштеги `#sunset`/`#ocean`/`#travel`/`#roadtrip`, упоминание alice→@bob (+уведомление `Mention`)

## Phase 14 — Комментарии: ответы (2 уровня) + лайки · §5 ✅
- [x] `PostComment.ParentCommentId (int?)` — максимум 2 уровня (ответ на ответ → к тому же родителю верхнего уровня: `parent.ParentCommentId ?? parent.Id`). Самоссылка `ON DELETE CASCADE` (удаление верхнего коммента уносит ответы)
- [x] Расширить `POST /Post/add-comment`: необязательный `parentCommentId`; авто-подстановка `@username` того, кому отвечаешь, в начало текста + `Mention` + уведомление `CommentReply` автору исходного коммента (пост для ответа берётся у родителя — источник истины)
- [x] Сущность `CommentLike { Id, CommentId, UserId, CreatedAt }` (unique `(CommentId,UserId)`); `POST /Post/like-comment?commentId` (тумблер) + уведомление `CommentLike`
- [x] `GET /Post/get-comment-replies?commentId&PageNumber&PageSize` (+блок-фильтр авторов ответов через `AccessGuard.BlockRelatedUserIds`); в DTO коммента: `parentCommentId`, `repliesCount`, `likesCount`, `isLiked` (переиспользуемая проекция `CommentProjections`)
- [x] Миграция `AddCommentRepliesAndLikes`; сид: ответ bob→@admin + лайк коммента alice→admin + уведомления `CommentReply`/`CommentLike`
- **Решение:** авто-`@` ответа создаёт запись `Mention` (кликабельная ссылка), но `Mention`-уведомление адресату подавляется (`suppressNotificationUserId` в `MentionService`) — он уже получает `CommentReply`, чтобы не дублировать. Ответ шлёт только `CommentReply` (не `Comment` автору поста). `CommentCount` поста по-прежнему считает все комменты, включая ответы (обратная совместимость)

## Phase 15 — Групповые чаты · §7 ✅
> Отдельная ветка от личных 1:1 — базовые Chat/Message не трогаем.

- [x] Сущности `GroupChat { Id, Name, Avatar?, CreatorUserId, CreatedAt }`, `GroupChatMember { Id, GroupChatId, UserId, Role(Admin/Member), JoinedAt, LastReadAt? }`, `GroupMessage { Id, GroupChatId, SenderUserId?, MessageText?, FileName?, MessageType, Duration?, Waveform?, ReplyToMessageId?, IsForwarded, CreatedAt }` (+`LastReadAt` для непрочитанных на участника; поля voice/reply/forward заведены под §8)
- [x] Enum `GroupMemberRole {Admin, Member}` + общий `MessageType {Text, Image, File, Voice, System}` (переиспользуется в §8); группа остаётся группой даже с 1 участником
- [x] **Системные сообщения** (`MessageType=System`, `SenderUserId=null`): создал/добавил/удалил/вышел/сменил название/назначен админом (текст с именами)
- [x] `GroupChatService` + SignalR-рассылка участникам (новый `GroupChatHub`/`IGroupChatNotifier`/`GroupChatNotifier`, `/groupChatHub`); рассылаются и обычные, и служебные сообщения
- [x] Эндпоинты: `POST /GroupChat/create`, `GET /get-my-groups`, `GET /get-group-by-id?groupId` (+пометить прочитанными), `POST /add-member`, `DELETE /remove-member`, `POST /promote-admin`, `POST /leave`, `PUT /update-info` (multipart: Name, Avatar), `PUT /send-message` (multipart: MessageText, File, ReplyToMessageId), `DELETE /delete-message?messageId`
- [x] Роли: Admin — управление участниками/инфо/промоут; Member — писать и выйти; при уходе последнего админа — авто-передача админства самому давнему участнику; блок-проверки при create/add-member
- [x] Миграция `AddGroupChats` (3 таблицы, unique `(GroupChatId,UserId)`, self-ref reply `SET NULL`) + сид: группа «Команда проекта» (admin + alice/bob) со служебными/текстовыми сообщениями и reply
- **Решение:** отдельный enum `GroupMessageType` не заводился — создан общий `MessageType` (Text/Image/File/Voice/System), который переиспользует Phase 16 для личных сообщений. `LastReadAt` на участнике — источник для подсчёта непрочитанных (не свои, не служебные, после времени последнего открытия). Reply-самоссылка на `SET NULL` (удаление процитированного не уносит ответы). Групповая отправка не делает попарных блок-проверок (доступ = членство), но добавление участника блок-проверяется против админа.

## Phase 16 — Сообщения: реакции, reply, forward, голосовые (личные + группы) · §8 ✅
- [x] Изменения `Message`: `ReplyToMessageId?` (self-ref `ON DELETE SET NULL`), `IsForwarded`, `MessageType(Text/Image/File/Voice)`, `Duration?`, `Waveform?` (у `GroupMessage` заведены ещё в Phase 15)
- [x] Enum `MessageType` (создан в Phase 15); enum `MessageContext {Direct, Group}`
- [x] Сущность `MessageReaction { Id, MessageId, MessageContext, UserId, Emoji, CreatedAt }` — **полиморфна** (MessageId → `Message`/`GroupMessage` по контексту, без FK на сообщение), unique `(MessageId, MessageContext, UserId)`; один юзер = одна реакция (повтор снимает, другой эмодзи заменяет)
- [x] `POST /Message/react?messageId&context&emoji` (тумблер/замена) + real-time пуш (`ReceiveReaction` на `/chatHub` и `/groupChatHub`); блок-проверка для Direct, членство для Group, запрет реакции на служебные
- [x] Reply: цитата исходного в DTO (`ReplyTo`: id/автор/текст/тип) для личных (`GetMessageDto`) и групповых; `POST /Chat/send-message` расширен необязательным `ReplyToMessageId`
- [x] Forward: `POST /Message/forward?messageId&context&targetChatId&targetContext` — копия содержимого (физическая копия файла), `IsForwarded=true`, без ссылки на оригинал; служебные пересылать нельзя
- [x] Голосовые: `POST /Message/send-voice` (multipart: Context, ChatId, File, Duration, ReplyToMessageId); `MessageType=Voice`, файл в `wwwroot/voice` (FileService расширен параметром папки + `CopyFile`), `Waveform` — детерминированный плейсхолдер-массив (`WaveformGenerator`); реал-тайм пуш обычным сообщением
- [x] Реакции отдаются в DTO сообщений (`Reactions[]` в `GetMessageDto`/`GetGroupMessageDto`) через `ReactionEnrichment` (батч, полиморфизм без навигации); очистка реакций при удалении сообщения/чата
- [x] Миграция `AddMessageReactionsAndVoice` (таблица `MessageReactions` + 5 колонок в `Messages`, дефолты для существующих строк) + сид: реакции в группе (admin ❤️, bob 🔥) и в личном чате alice↔bob (bob 😂) + личный чат с reply

## Phase 17 — Сторис: close friends, ответы, репост поста · §9 ✅
- [x] Сущность `CloseFriend { Id, UserId, FriendUserId, CreatedAt }` (направленная, unique `(UserId, FriendUserId)`, две FK на юзера каскадом); `Story.Audience (enum StoryAudience All/CloseFriends)`, `Story.SharedPostId (int?, FK Posts `SET NULL`)` — отдельно от `Story.PostId` (сторис-из-поста)
- [x] Эндпоинты CloseFriend: `POST /CloseFriend/add?userId`, `DELETE /CloseFriend/remove?userId`, `GET /CloseFriend/get-list?PageNumber&PageSize` (идемпотентные add/remove; блок-проверка при add; пагинация с `IsVerified` в DTO)
- [x] Публикация сторис с `audience` (необязательное поле в `AddStoryDto`); в `get-stories`/`get-user-stories` фильтр close-friends по членству зрителя (свои close-friends-сторис видны себе); `GetStoryById` тоже проверяет членство
- [x] Ответы на сторис: `POST /Story/reply?storyId` (body `{ text }`) → личное сообщение автору (чат создаётся при отсутствии) + сущность `StoryReply { Id, StoryId, FromUserId, MessageId, CreatedAt }` + уведомление `StoryReply` + real-time пуш (`IChatNotifier`); учёт `WhoCanReplyStory` (Everyone/Followers/CloseFriends/Nobody), только активная (< 24ч) чужая сторис, блок-проверка; проводка `MentionEntityType.StoryReply` (задел Phase 13)
- [x] Репост поста: `POST /Story/share-post?postId` → сторис с `SharedPostId` (только публичные посты, блок-проверка) + уведомление `PostShared`; в `GetStoryDto` — превью репоста (`SharedPost`: postId/author/image)
- [x] Миграция `AddStoryCloseFriendsAndReplies` (2 колонки в `Stories` + таблицы `CloseFriends`/`StoryReplies`) + сид: alice-сторис (All + CloseFriends), close-friend alice→bob, ответ bob на сторис alice (сообщение+StoryReply), репост поста alice в сторис bob, уведомления `StoryReply`/`PostShared`
- **Решение:** `SharedPostId` заведён отдельным полем от существующего `Story.PostId` — это разные сценарии (репост-превью vs. сторис, собранная из поста). Ответ на сторис переиспользует логику чата (`GetOrCreateChatAsync` внутри `StoryService`, нормализованный порядок участников, дедуп), сообщение — обычный Direct `MessageType.Text`, связка `StoryReply` через nav (одна `SaveChanges`). Блок/приватность на самих выдачах сторис (`get-stories`/`get-user-stories`) в рамках §9 не расширялись — только фильтр close-friends; сквозная блок/приват-проверка сторис отнесена к Phase 22 (харденинг)

## Phase 18 — Real-time статусы: presence (взаимная приватность) + typing · §1
- [ ] Presence: **взаимная** логика `ShowOnlineStatus` — если у запрашивающего false, он не видит чужие статусы и его статус скрыт; отдавать `isOnline` + `lastSeen (DateTime?)`
- [ ] Typing в личном чате: `UserTyping{chatId, userId, userName, kind}`, `kind ∈ {text, voice}`
- [ ] Typing в группах: один печатает → имя; несколько → список печатающих
- [ ] `kind=voice` при записи голосового; событие эфемерное (в БД не пишем)

## Phase 19 — Верификация (Admin) · §10
- [ ] `User.IsVerified`; `AdminController` со всеми методами `[Authorize(Roles="Admin")]`
- [ ] Эндпоинты: `POST /Admin/verify-user?userId`, `DELETE /Admin/unverify-user?userId`, `POST /Admin/grant-admin?userId`, `DELETE /Admin/revoke-admin?userId`
- [ ] `isVerified` во всех DTO пользователя/автора (профиль, автор поста/коммента/сторис)
- [ ] Миграция

## Phase 20 — Двухфакторная аутентификация (TOTP + email-код + backup codes) · §11
- [ ] `User.TwoFactorEnabled`, `User.TwoFactorSecret`; сущность `BackupCode { Id, UserId, CodeHash, IsUsed }`
- [ ] TOTP через `Otp.NET`/Identity: генерация секрета, `otpauth://`-URI + строка для QR
- [ ] Email-код (6 цифр, TTL 5–10 мин); пачка одноразовых резервных кодов при включении
- [ ] Флоу логина: `/Account/login` при 2FA возвращает `twoFactorToken` (не JWT); `POST /Account/login-2fa { twoFactorToken, code, method(Totp/Email/Backup) }` → JWT
- [ ] Управление: `POST /enable-2fa`, `POST /confirm-2fa?code`, `POST /disable-2fa?code`, `POST /send-2fa-email`, `POST /regenerate-backup-codes`
- [ ] Миграция

## Phase 21 — Explore / рекомендации (content-based) · §12
- [ ] Профиль интересов: сущность `UserInterest` (агрегат) или расчёт на лету; веса действий (favorite > comment > like > view) + затухание по времени
- [ ] Агрегация любимых хэштегов и авторов из взаимодействий
- [ ] Скоринг кандидатов: `score = w1·хэштеги + w2·близость_автора + w3·популярность + w4·свежесть`
- [ ] Фильтры: свои посты, уже просмотренные (`PostView`), блок в любую сторону, приватные без `Accepted`, авторы на которых уже подписан
- [ ] Разнообразие: не более N подряд от одного автора, перемешивание топа
- [ ] Эндпоинты: `GET /Explore/get-feed`, `GET /Explore/get-popular` (cold start)
- [ ] Миграция (если `UserInterest` материализуется)

## Phase 22 — Качество, харденинг, документация (фичи) · §13
- [ ] Сквозная проверка блокировок и приватности во всех новых и затронутых выдачах
- [ ] Ревизия: не создаются уведомления/mention на собственные действия
- [ ] Ревизия авторизации владельца/ролей/групп-ролей
- [ ] Пагинация и валидаторы на всех новых списках/DTO; безопасная загрузка voice/аватара группы
- [ ] Обновить сид: роли, приватный аккаунт, группа, тестовые уведомления/хэштеги
- [ ] Swagger: теги новых контроллеров, описания
- [ ] Обновить README: новые эндпоинты и фичи
- [ ] Ручной smoke-тест новых групп эндпоинтов (на живом PostgreSQL)
- [ ] Финальная сборка без предупреждений
