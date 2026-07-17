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
- **Активная фаза:** Модуль **Прямых эфиров (Live Streaming)** по [`instagram-backend-live-prompt.md`](./instagram-backend-live-prompt.md) — **завершён**: сборка 🟢 (0/0), миграция `AddLiveStreaming` (6 таблиц + индексы), **живой smoke на PostgreSQL пройден 34/35** (одноразовый кластер PG17, Fake-провайдер; единственный «fail» — сабсекундная длительность эфира, не дефект). Провайдер `IStreamingProvider` (LiveKit + Fake), `LiveStreamController` (27 эндпоинтов), SignalR `LiveHub`, вебхуки с проверкой подписи, фоновое автозавершение, сид, README, `docker-compose.livekit.yml`.
- **Проверено на реальном LiveKit Cloud (2026-07-17):** `Streaming:Provider=LiveKit` с боевыми ключами (хранятся в **user-secrets**, не в гите) — `POST /Live/start` отдаёт настоящий HS256-JWT (`iss`=ApiKey, `canPublish: true`), `POST /Live/join` — токен зрителя (`canPublish: false`) в той же комнате, `serverUrl` = `wss://…livekit.cloud`; `CreateRoom` через Twirp Server API → HTTP 200. Осталось только клиентское приложение с `livekit-client` — бэкенд к видео готов.
- **Прежняя активная фаза:** Phase 23 — Активные сеансы + Refresh-токены (access+refresh) — **завершена**: сборка 🟢, миграция применена, **живой smoke на PostgreSQL пройден 27/27** (одноразовый кластер PG17). Phase 22 (харденинг/документация) закрыта.
- **Последняя сессия:** 2026-07-16 (27) — Phase 23, активные сеансы + refresh-токены. Переход на схему **access (15 мин) + refresh (30 дней, ротация + reuse-detection)**: сущность `UserSession` + миграция `AddUserSessions`, `SessionService`, `SessionValidationMiddleware` (мгновенный отзыв + троттлинг активности), определение устройства (UAParser + X-Forwarded-For), геолокация-заглушка, уведомление `NewLogin`, отзыв прочих сессий при смене/сбросе пароля и отключении 2FA, фоновая очистка. Эндпоинты `/Account/refresh-token`, `/Account/logout`, `/Session/*`. `login`/`login-2fa`/`register` теперь отдают пару токенов `AuthResultDto`. **Верификация без БД харнессом** реального кода определения устройства/хэшера/троттлинга — найден и исправлен баг классификации macOS как Mobile. README/конфиг обновлены.
- **Прежняя сессия:** 2026-07-14 (26) — Phase 22, харденинг + документация. **Найдены и закрыты 3 реальных бага:** (1) SignalR `access_token` из query извлекался только для `/chatHub`+`/notificationHub` → добавлены `/groupChatHub`+`/presenceHub` (их WS-подключения не аутентифицировались); (2) сторис по юзеру/по id (`GetUserStoriesAsync`/`GetStoryByIdAsync`) фильтровали только close-friends → добавлен `AccessGuard.CanViewContentAsync` (блок в обе стороны + приватный без Accepted); (3) лента сторис (`GetStoriesAsync`) брала авторов без фильтра статуса → pending-запрос к приватному показывал его сторис до одобрения → `Status == Accepted`. Проведена ревизия остальных инвариантов §13 — «не себе» (уведомления/mention), авторизация владельца/ролей/групп-ролей, пагинация+валидаторы, безопасная загрузка voice/аватара — нарушений не найдено. README расширен всеми эндпоинтами и фичами Phase 11–21 + таблица SignalR-хабов + обзор фич.
- **Следующий шаг:** коммит Phase 23 + лог сессии (по `/stop`). Опционально — отложенный smoke Phase 22 (сторис-хардненинг / WS-хабы) на живой БД.
- **Состояние сборки:** 🟢 зелёная (0 предупреждений, 0 ошибок). Проверено: `dotnet build` чистый; миграция `AddUserSessions` (только таблица `UserSessions`, 4 индекса, FK каскад) применяется на PostgreSQL (11 миграций, 0 pending). **Живой smoke Phase 23 — 27/27 assertions** (login/сессии/NewLogin/refresh-ротация/reuse-detection/мгновенный отзыв/logout/ownership-guard/change-password). Пуре-логика (UAParser-классификация, refresh-хэшер, троттлинг) дополнительно прогнана харнессом.

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

## Phase 18 — Real-time статусы: presence (взаимная приватность) + typing · §1 ✅
- [x] `User.LastSeen (DateTime?)` + миграция `AddUserLastSeen` (одна колонка в `AspNetUsers`); обновляется на переходе в офлайн (последнее соединение любого хаба)
- [x] Presence: **взаимная** логика `ShowOnlineStatus` — если у запрашивающего false, он не видит чужие статусы и его статус скрыт для всех; отдавать `isOnline` + `lastSeen (DateTime?)` (+ фильтр по блокировке и скрытию цели). `PresenceService.GetStatusAsync`/`GetStatusesAsync`, эндпоинты `GET /Presence/get-status?userId`, `POST /Presence/get-statuses { userIds }`
- [x] In-memory `IPresenceTracker` (singleton): онлайн = ≥1 активное SignalR-соединение любого хаба (учёт connectionId); базовый `PresenceAwareHub<T>` кормит его из `OnConnected/OnDisconnected` во всех 4 хабах
- [x] Real-time рассылка изменений статуса: новый `/presenceHub` (`IPresenceClient.ReceivePresence`), адресно собеседникам по личным чатам ∪ участникам общих групп (с фильтром блок/взаимное скрытие)
- [x] Typing в личном чате: `ChatHub.Typing(chatId, kind)` → событие `UserTyping{chatId, userId, userName, kind}` собеседнику; `kind ∈ {text, voice}`; проверка участия + блокировки
- [x] Typing в группах: `GroupChatHub.Typing(groupChatId, kind)` → `GroupTyping{groupChatId, typers[]}` остальным участникам (эфемерный список из `ITypingTracker`, TTL 6с); один печатает → имя, несколько → список
- [x] `kind=voice` при записи голосового (нормализация kind сервером); событие эфемерное — в БД не пишем
- **Решение:** presence/typing — real-time (SignalR), не REST (кроме query-эндпоинтов статуса). Presence-трекер и typing-трекер — эфемерные in-memory singletons (переживать рестарт не нужно; онлайн определяется живыми соединениями). Все хабы (chat/group/notification/presence) наследуют `PresenceAwareHub<T>`, поэтому «онлайн» = приложение открыто и держит хотя бы одно соединение (обычно notificationHub). `TypingService` — best-effort: при недопустимом вводе (не участник, блок, чужой чат) молча выходит, не бросает (это «сигнал», а не команда). Групповой typing отдаёт весь список печатающих (а не отдельные события) — сервер собирает его в `TypingTracker` с TTL; клиент авто-сбрасывает индикатор ~3с. Рассылка presence адресная (собеседники ∪ co-members), а не всем — приватность и масштаб.

## Phase 19 — Верификация (Admin) · §10 ✅
- [x] `User.IsVerified` (уже с Phase 11); `IAdminService`/`AdminService` + `AdminController` со всеми методами `[Authorize(Roles = DbInitializer.AdminRole)]`
- [x] Эндпоинты: `POST /Admin/verify-user?userId`, `DELETE /Admin/unverify-user?userId`, `POST /Admin/grant-admin?userId`, `DELETE /Admin/revoke-admin?userId` (verify/unverify — флаг `User.IsVerified` через `UserManager.UpdateAsync`; grant/revoke — роль Admin через `AddToRole`/`RemoveFromRole`; идемпотентные проверки «уже в целевом состоянии», защита от самолокаута на revoke-admin)
- [x] `isVerified` во всех DTO пользователя/автора — **дозаведено в этой фазе**: `GetUserProfileDto` (профиль), `GetPostDto` (автор поста, через `PostProjections`), `GetPostCommentDto` (автор коммента, через `CommentProjections`), `GetStoryDto` (автор сторис, через `StoryProjections`), `GetUserDto` (поиск/подписчики/подписки/заблокированные). Ранее `isVerified` был только в новых DTO фаз 12–15 (CloseFriend/FollowRequest/GroupMember/NotificationActor) — базовые author-DTO его не отдавали
- [x] Миграция не требуется: колонка `IsVerified` в `AspNetUsers` уже существует с Phase 11 (`AddNotifications`). Сид: `admin` и `alice` помечены `IsVerified=true`
- **Решение:** AutoMapper-профиль (`MappingProfile`) в проекте зарегистрирован, но **не используется** ни одним сервисом (нет инъекций `IMapper`) — все DTO собираются вручную/через выражения-проекции; поэтому `isVerified` добавлялся в проекции и ручные `new …Dto`, а не в `CreateMap`. `GetStoriesDto`/`StoryItemDto` — мёртвый код (нигде не используется, лента сторис отдаёт `List<GetStoryDto>`), не трогали.

## Phase 20 — Двухфакторная аутентификация (TOTP + email-код + backup codes) · §11 ✅
- [x] `User.TwoFactorSecret (string?)` (флаг `TwoFactorEnabled` — стандартный из `IdentityUser`, колонка уже есть); сущность `BackupCode { Id, UserId, CodeHash, IsUsed, CreatedAt }` (в БД только SHA-256-хэш, код показывается один раз)
- [x] TOTP собственной реализацией RFC 6238 (`ITotpService`/`TotpService`, HMAC-SHA1, 6 цифр, шаг 30с, ±1 окно) + Base32: генерация секрета, `otpauth://`-URI + `ManualEntryKey` для QR. Проверено на 5 официальных тест-векторах RFC 6238 (все PASS)
- [x] Email-код (6 цифр, TTL 10 мин) + пачка из 10 одноразовых резервных кодов при включении/регенерации; эфемерное состояние login-флоу (токены сессии + email-коды) — in-memory singleton `ITwoFactorTokenStore` (как presence/typing-трекеры)
- [x] Флоу логина: `/Account/login` при 2FA возвращает `TwoFactorRequiredDto { requiresTwoFactor, twoFactorToken, methods[] }` вместо JWT (тип расширен до `Response<object>` — для не-2FA по-прежнему JWT-строка, контракт базы неизменен); `POST /Account/login-2fa { twoFactorToken, code, method(Totp/Email/Backup) }` → JWT
- [x] Управление: `POST /enable-2fa` (секрет/QR + резервные коды), `POST /confirm-2fa?code`, `POST /disable-2fa?code` (TOTP или резервный код), `POST /send-2fa-email` (аноним, по `twoFactorToken`), `POST /regenerate-backup-codes`
- [x] Миграция `Add2FA` (колонка `TwoFactorSecret` + таблица `BackupCodes` с индексом `(UserId, IsUsed)`); сид: демо-аккаунт `frank` с включённой 2FA (фикс. TOTP-секрет + 3 известных резервных кода для проверки login-флоу без генератора)
- **Решение:** TOTP реализован вручную (без `Otp.NET`) — алгоритм детерминированный, полностью покрыт стандартной криптографией платформы (`HMACSHA1`), не тянет внешнюю зависимость и офлайн-безопасен (спец допускает «библиотеку **типа** Otp.NET»); секрет живёт в `User.TwoFactorSecret` (Base32), как требует §11. `enable-2fa` не активирует 2FA сразу — секрет проверяется первым кодом в `confirm-2fa` (флаг `TwoFactorEnabled` ставится там). `method` в login-2fa принимается строкой и парсится без учёта регистра (глобального `JsonStringEnumConverter` в проекте нет). Резервные коды хранятся хэшами (SHA-256), нормализация ввода (без дефисов, верхний регистр). Email-код не отправляется реально — пишется в лог и возвращается в `data` (учебные цели, как reset-токен в `ForgotPassword`).

## Phase 21 — Explore / рекомендации (content-based) · §12 ✅
- [x] Профиль интересов **расчётом на лету** (без материализованного `UserInterest`): веса действий (favorite=4 > comment=3 > like=2 > view=1) × экспоненциальное затухание по времени (период полураспада 30 дней; просмотры без отметки времени — без затухания). Вес поста разворачивается в веса хэштегов и авторов
- [x] Агрегация любимых хэштегов (`HashtagId`) и авторов из взаимодействий (`BuildInterestProfileAsync`)
- [x] Скоринг кандидатов `score = 0.40·хэштеги + 0.30·близость_автора + 0.15·популярность + 0.15·свежесть`; компоненты хэштеги/автор/популярность нормируются в [0..1] по максимуму пула, свежесть — экспоненциальное затухание (полураспад 7 дней)
- [x] Фильтры: свои посты (`p.UserId != currentId`), уже просмотренные (`PostView`), блок в любую сторону + приватные без `Accepted` (переиспользован `AccessGuard.VisibleTo`), авторы на которых уже подписан (`Accepted`)
- [x] Разнообразие: не более 2 постов подряд от одного автора (`Diversify` — жадное переупорядочивание сортированного по score списка)
- [x] Эндпоинты: `GET /Explore/get-feed`, `GET /Explore/get-popular` (cold start — чистая популярность лайки+комменты+просмотры, свежесть тай-брейк)
- [x] Миграция не требуется (профиль считается на лету — новых сущностей нет). Сид расширен Explore-контентом: посты авторов вне подписок alice (`carol`/`frank`) с хэштегами `#sunset`/`#travel` + новые теги `photography`/`nature`/`beach`/`mountains`/`city`; взаимодействия alice (favorite/like/view) формируют интересы; популярность на кандидатах
- **Решение:** профиль интересов считается **на лету** (спец §12 явно допускает «или расчёт на лету»), без таблицы `UserInterest` — нет проблемы устаревания агрегата и лишней миграции (как Phase 19); согласуется со стилем проекта (DTO собираются проекциями, а не материализованными агрегатами). Персональное ранжирование делается in-memory над ограниченным пулом кандидатов (`MaxCandidates=500`, свежайшие сверху), чтобы не тянуть всю таблицу; хэштеги грузятся отдельным батч-запросом (без коллекционной проекции в SQL — гарантированная трансляция). Перемешивание топа — детерминированное (свежесть уже в score, диверсификация по авторам), без рандома: иначе страницы пагинации перекрывались бы/теряли посты. «Холодный» юзер без истории в `get-feed` деградирует к популярности+свежести автоматически (компоненты хэштегов/автора обнуляются) — `get-popular` остаётся явным cold-start фолбэком. `get-popular` НЕ исключает просмотренные/подписки (только свои/блок/приват) — это витрина популярного, а не «открытие нового».

## Phase 22 — Качество, харденинг, документация (фичи) · §13 (в работе)
- [x] Сквозная проверка блокировок и приватности во всех новых и затронутых выдачах — **найдены и закрыты 3 бага**: (1) `StoryService.GetUserStoriesAsync`/`GetStoryByIdAsync` фильтровали только close-friends → добавлен `AccessGuard.CanViewContentAsync` (блок в обе стороны + приватный без Accepted → пустая лента / 404); (2) `GetStoriesAsync` брал авторов ленты без фильтра статуса → pending-запрос к приватному аккаунту показывал его сторис до одобрения → фильтр `Status == Accepted`. Остальные выдачи (`get-following-post`, hashtag, explore) подтверждены — прикрыты `VisibleTo`
- [x] Ревизия «не создаются уведомления/mention на собственные действия» — подтверждено: `NotificationService.CreateAsync` отсекает `recipientUserId == actorUserId`; `MentionService` исключает автора (`u.Id != authorUserId`) в самом запросе кандидатов
- [x] Ревизия авторизации владельца/ролей/групп-ролей — подтверждено: `/Admin/*` под `[Authorize(Roles=Admin)]`, групп-действия по роли участника, удаление/правка ресурсов только владельцем
- [x] Пагинация и валидаторы на всех новых списках/DTO; безопасная загрузка voice/аватара группы — подтверждено: пагинация на всех новых списках; валидаторы для новых body-DTO (Login2Fa/Send2FaEmail/CreateGroupChat/SendGroupMessage/UpdateGroupInfo/SendVoice/UpdatePrivacy/StoryReply); voice → белый список расширений+15МБ+папка `voice`, вложения группы → 25МБ, аватар группы → дефолтные image-ограничения
- [x] **Фикс (реальный баг):** SignalR `access_token` из query извлекался только для `/chatHub` и `/notificationHub` — добавлены `/groupChatHub` и `/presenceHub` (иначе их WebSocket-подключения не аутентифицировались, хотя doc-комментарии хабов это обещали)
- [x] Сид: роли, приватный аккаунт (`diana`), группа, уведомления/хэштеги/2FA — уже наполнен в фазах 12–20; новых пробелов не выявлено (описан в README)
- [x] Swagger: теги новых контроллеров, описания — подтверждено: все новые контроллеры имеют class-level XML `<summary>` (тег = имя контроллера), Bearer-схема глобально
- [x] Обновить README: новые эндпоинты и фичи — добавлены секции Notification/Settings/Block/Hashtag/CloseFriend/GroupChat/Message/Presence/Admin/Explore + 2FA (Account) + follow-requests + reply/share-post (Story) + таблица SignalR-хабов + обзор фич Phase 11–21; обновлены сид-аккаунты (diana/frank) и раздел авторизации (2FA/приватность)
- [ ] Ручной smoke-тест новых групп эндпоинтов (на живом PostgreSQL) — требует `DATABASE_URL`/деплоя (в этой сессии нет БД); сид готовит проверяемый сценарий
- [x] Финальная сборка без предупреждений — 🟢 0 warnings / 0 errors

## Phase 23 — Активные сеансы + Refresh-токены (access + refresh) ✅
- [x] Схема аутентификации переведена на **access (JWT, 15 мин) + refresh (30 дней)**. `AccessTokenLifetimeMinutes`/`RefreshTokenLifetimeDays`/`MaxActiveSessionsPerUser` в секции `Jwt`; в access-токен добавлен claim `sessionId`. `LifetimeMinutes` осталось легаси-полем
- [x] Сущность `UserSession` (Guid Id, UserId, RefreshTokenHash+PreviousRefreshTokenHash, DeviceName/DeviceType/Browser/OS, IpAddress, Location, CreatedAt/LastActivityAt/ExpiresAt, IsRevoked/RevokedAt) + `DeviceType` enum (Unknown/Mobile/Desktop/Web). Refresh хранится **только хэшем** (SHA-256), в открытом виде — один раз. Миграция `AddUserSessions` (индексы UserId, RefreshTokenHash, PreviousRefreshTokenHash, ExpiresAt; FK каскад)
- [x] `ISessionService`: `CreateSessionAsync` (логин/регистрация/2FA), `RefreshAsync` (**ротация** + **reuse-detection**: предъявление ротированного/отозванного refresh → отзыв всех сессий юзера + 401), `LogoutAsync`, `GetActiveSessionsAsync`, `RevokeSessionAsync`, `RevokeAllOthersAsync`, `RevokeAllForUserAsync`/`RevokeAllOtherForCurrentAsync`, `ValidateAndTouchAsync`
- [x] Флоу логина: `login` (без 2FA), `login-2fa`, `register` возвращают `AuthResultDto { accessToken, refreshToken, expiresIn, sessionId }` (при 2FA сессия создаётся только после второго фактора). Новые эндпоинты: `POST /Account/refresh-token` 🔓, `POST /Account/logout`, `GET /Session/get-active-sessions`, `DELETE /Session/revoke-session?sessionId`, `DELETE /Session/revoke-all-others`
- [x] Определение устройства: `DeviceInfoService` парсит `User-Agent` через **UAParser** (Browser/OS/DeviceType/DeviceName «Chrome на Windows»), IP — из `RemoteIpAddress` с приоритетом `X-Forwarded-For`. Геолокация — абстракция `IGeoLocationService` (заглушка, при недоступности `null`, флоу не ломает)
- [x] `SessionValidationMiddleware` на каждом авторизованном запросе проверяет отзыв/истечение сессии (мгновенный отзыв, не через 15 мин) и **троттлингом** (`ISessionActivityThrottle`, окно 5 мин, in-memory singleton) обновляет `LastActivityAt` — без удара по БД на каждом запросе
- [x] Связь с модулями: вход с нового устройства/IP → уведомление `NotificationType.NewLogin` (+ SignalR, не при первом входе после регистрации); `ChangePassword`/`disable-2fa` → отзыв прочих сессий; `ResetPassword` → отзыв всех. *(Бан админом/удаление аккаунта в кодовой базе пока отсутствуют — при добавлении вызвать `RevokeAllForUserAsync`.)*
- [x] Фоновая `SessionCleanupService` (BackgroundService, раз в сутки): удаляет истёкшие и отозванные >30 дней сессии (`ExecuteDeleteAsync`)
- [x] Сид: две демо-сессии для `alice` (десктоп-веб + мобильная) — `GET /Session/get-active-sessions` не пустой
- [x] Обновлён README (схема access+refresh, изменение контракта `login`/`register`, эндпоинты Session, конфиг Jwt). Сборка 🟢 0/0
- [x] Верификация без БД: реальный код `DeviceInfoService`/`RefreshTokenHasher`/`SessionActivityThrottle` прогнан харнессом (класс. Web/Mobile/Unknown, X-Forwarded-For, ротация хэшей, окно троттлинга) — **найден и исправлен баг**: десктопный macOS (UAParser `Device.Family = "Mac"`) ошибочно классифицировался как `Mobile` из-за широкого правила «любое непустое семейство устройства» → сужено до явных мобильных признаков
- [x] **Фикс (реальный баг мультидевайса):** `SessionValidationMiddleware` проверял сессию и на `[AllowAnonymous]`-эндпоинтах → если клиент слал ещё-валидный access-токен с уже отозванной сессией (после logout/revoke-all-others/смены пароля/reuse-detection/лимита), повторный `POST /Account/login` возвращал 401 — пользователь не мог перелогиниться, пока не истечёт старый токен. Теперь на анонимных эндпоинтах (login/register/login-2fa/refresh-token) сессия не валидируется; на защищённых мгновенный отзыв сохранён. Регресс-тест 12/12 (перелогин со stale-токеном, мультидевайс, 3 устройства одновременно, сохранение instant-revocation)
- [x] **Живой smoke на PostgreSQL пройден (27/27 assertions):** поднят одноразовый кластер PG17 (trust-auth) на свободном 5432, миграции применились (11, 0 pending), сид отработал. Проверено: login→`AuthResultDto` (expiresIn 900, claim `sessionId` в JWT); get-active-sessions (2 сид-сессии + текущая, текущая первой, isCurrent); уведомление `NewLogin` (type 10) при входе с нового устройства; refresh (ротация, sessionId сохранён); **reuse-detection** старого refresh → 401 + отзыв всех сессий; **мгновенный отзыв** — старый access-токен → 401 через middleware; logout → 401; revoke-session (своя 200 / чужая 403); change-password отзывает прочие сессии, текущую сохраняет. Кластер после теста снесён, 5432 снова свободен, БД пользователя не затронуты

---

# 🎥 Прямые эфиры (Live Streaming) — [`instagram-backend-live-prompt.md`](./instagram-backend-live-prompt.md)

> Модуль встроен в тот же проект (Domain/Infrastructure/WebApi, `Response<T>`, JWT, миграции). Видео идёт
> напрямую клиент↔LiveKit, минуя бэкенд; бэкенд раздаёт токены доступа и ведёт всю бизнес-логику.

## Live Streaming — модуль (в работе)
- [x] Абстракция провайдера `IStreamingProvider` + `enum ParticipantRole {Subscriber, Publisher}`; реализации `LiveKitStreamingProvider` (JWT-токены доступа HS256 с grants roomJoin/canPublish/canSubscribe вручную для вложенного `video`-claim; Server API через Twirp `RoomService` best-effort) и `FakeStreamingProvider` (фиктивные токены, no-op управление). Выбор по конфигу `Streaming:Provider` (+ авто-фолбэк на Fake, если ключи LiveKit не заданы)
- [x] Конфигурация `StreamingOptions` (секция `Streaming`: Provider, LiveKit{Url,ApiKey,ApiSecret,TokenLifetimeMinutes}, MaxGuests=3, MaxCommentLength=200); `appsettings.json` наполнен (Provider=Fake по умолчанию — запускается локально без ключей)
- [x] Сущности `LiveStream`/`LiveViewer`/`LiveComment`/`LiveLike`/`LiveGuestRequest`/`LiveBan` + enum'ы `LiveStreamStatus`/`LiveStreamAudience`/`LiveGuestRequestStatus`; EF-конфиги (RoomName unique, индексы UserId/Status, (LiveStreamId,UserId), (LiveStreamId,Status); FK на эфир каскадом, на юзера — Restrict). Миграция `AddLiveStreaming` (6 таблиц)
- [x] `NotificationType` расширен `LiveStarted=11`/`LiveGuestRequest=12`, `NotificationEntityType` — `LiveStream=4` (append, коды стабильны)
- [x] DTO (запросы + ответы + payload'ы real-time событий) + валидаторы (`StartLive`/`UpdateLiveTitle`/`AddLiveComment`); бизнес-лимит длины коммента (`MaxCommentLength`) проверяется в сервисе
- [x] `LiveStreamService`: старт (запрет второго активного, токен Publisher, уведомление подписчикам `LiveStarted` + SignalR `StreamStarted` с учётом аудитории/блокировок/close friends), стоп (закрытие комнаты, фиксация `WatchDurationSeconds`, `StreamEnded` + статистика), смена заголовка
- [x] Просмотр: `get-active` (эфиры подписок с фильтром приватность/блок/close friends), `get-stream-by-id`, `join` (проверки бан/блок/аудитория → `LiveViewer`, инкремент `ViewersTotal`/пересчёт `ViewersPeak`, токен Subscriber, события `ViewerJoined`+`ViewerCount`), `leave`
- [x] Гости (лимит `MaxGuests`, очередь Pending): `request-guest` (проверки: не хост/не гость/не забанен/не заблокирован/нет активной заявки), `cancel-guest-request`, `get-guest-requests` (хост), `approve-guest` (**сериализуемая транзакция** от гонки одновременного одобрения при лимите + `UpdateParticipantRoleAsync(Publisher)` + `GuestJoined`), `decline-guest` (`GuestRequestDeclined` заявителю), `remove-guest` (понижение до Subscriber + `GuestLeft`), `get-active-guests`
- [x] Комментарии/реакции: `add-comment` (троттлинг + `MaxCommentLength` + `NewComment`), `delete-comment` (soft-delete автор/хост + `CommentDeleted`), `pin-comment` (один закреплённый + `CommentPinned`), `get-comments` (история, без удалённых), `send-like` (сердечко, троттлинг `ILiveRateLimiter`, инкремент `LikesCount`, `NewLike`)
- [x] Модерация: `ban-viewer` (`LiveBan` + `RemoveParticipantAsync` + понижение гостя + `ViewerBanned`), `unban-viewer`, `get-viewers` (хост, текущие зрители)
- [x] Статистика: `get-stats` (live — текущие/пик/уникальные/комменты/лайки/гости; после эфира — длительность/средняя длительность просмотра/суммарное время/топ-комментаторы/число заявок), `get-my-streams`; денормализованные счётчики (`ViewersPeak`/`ViewersTotal`/`LikesCount`/`CommentsCount`) — инкрементами
- [x] После эфира: `save-to-story` (при наличии `RecordingUrl`, флаг `SavedToStory`, наследование аудитории)
- [x] SignalR `LiveHub` (группа `live_{streamId}`, методы `JoinStream`/`LeaveStream`, **грейс-период 30с** при обрыве связи через `ILiveConnectionTracker` + fire-and-forget scope) + `ILiveNotifier`/`LiveNotifier` (WebApi); `/liveHub` добавлен в извлечение `access_token` из query; наследует `PresenceAwareHub` (учёт присутствия)
- [x] Вебхуки `POST /Live/webhook` `[AllowAnonymous]` + **обязательная проверка подписи** (`LiveKitWebhookValidator`: подпись токена `ApiSecret` + сверка SHA-256 тела с claim `sha256`; `FakeLiveWebhookValidator` для dev). События `room_finished` (автозавершение) / `participant_left` (фиксация выхода) идемпотентны
- [x] Фоновая `LiveStreamCleanupService` (BackgroundService, раз в 2 мин): автозавершение висящих эфиров (без активности >15 мин или длительность >12ч)
- [x] Интеграция: уведомления `LiveStarted`/`LiveGuestRequest`, close friends (`Audience=CloseFriends`), приватность/блокировки (через `AccessGuard`), `isVerified` в DTO, сессии (доступ через существующий middleware)
- [x] Сид: завершённый эфир alice (зрители bob/carol/admin, комменты, лайки, одобренный гость) + активный эфир bob (виден alice в `get-active`); README-секция про эфиры (ключи LiveKit Cloud, self-hosted `docker-compose.livekit.yml`+`livekit.yaml`, переключение провайдера, схема потоков) + таблица эндпоинтов + строка `/liveHub`
- [x] Сборка 🟢 0/0; миграция `AddLiveStreaming` создана
- [x] **Живой smoke на PostgreSQL пройден (34/35 assertions, Fake-провайдер):** одноразовый кластер PG17 (trust-auth, свободный 5432), 12 миграций применились (0 pending), сид отработал. Проверено: сид-эфиры (`get-my-streams`: ended-эфир alice с likes=5/comments=2/viewersTotal=3; `get-active`: активный эфир bob виден alice-подписчику, currentViewers=1); `start` (Publisher fake-токен, roomName `live_*`); `get-stream-by-id`; `join` (Subscriber-токен); `add-comment`/`send-like`; live-`get-stats` (currentViewers=1, comments=1, likes=1, unique=1); гости (`request-guest` Pending → `get-guest-requests` → `approve-guest` → `get-active-guests` carol); `get-viewers`; `ban-viewer` → carol не может ни join, ни comment (403); `end` (статус Ended); post-`get-stats` (unique≥1, топ-комментатор carol); `save-to-story` без записи → 400; запрет второго активного эфира → 400; **вебхук `room_finished` (Fake-валидатор) автозавершил эфир**. Единственный «fail» — `durationSeconds=0`: эфир стартовал и завершился в пределах одной секунды (целые секунды → 0), не дефект. Кластер снесён, 5432 снова свободен, БД пользователя не затронуты
- **Решения:** LiveKit-токены собираются вручную (base64url header/payload + HMAC-SHA256), т.к. вложенный grant `video` должен быть JSON-объектом — `JwtSecurityToken` сериализует claim'ы строками. Server API (create/update-role/remove/close room) — best-effort (сбой не ломает логику: видео вне бэкенда, LiveKit сам создаёт/закрывает комнату). `Fake`-провайдер — провайдер по умолчанию, чтобы проект запускался и тестировался локально без ключей и реального WebRTC. Троттлинг сердечек/комментов и трекер соединений — эфемерные in-memory singletons (как presence/typing). `save-to-story` кладёт внешний URL записи в `Story.FileName` (у сторис нет отдельного поля URL; контракт не трогаем). Аудитория эфира (`All`/`CloseFriends`) в теле `start` биндится числом (глобального string-enum-конвертера в проекте нет — как и у прочих enum'ов)
