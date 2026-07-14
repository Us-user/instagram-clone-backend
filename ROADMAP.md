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
- **Активная фаза:** Phase 16 — Сообщения: реакции/reply/forward/голосовые — **не начата**. Phase 15 (Групповые чаты) завершена.
- **Последняя сессия:** 2026-07-14 (19) — реализована Phase 15: сущности `GroupChat`/`GroupChatMember`(+`LastReadAt`)/`GroupMessage` + enum'ы `GroupMemberRole {Admin, Member}` и общий `MessageType {Text, Image, File, Voice, System}`; 3 EF-конфигурации (unique `(GroupChatId,UserId)`, FK каскадом, self-ref reply `ON DELETE SET NULL`) + DbSet'ы + миграция `AddGroupChats`; `GroupChatProjections` (список групп с непрочитанными по `LastReadAt`, участник→DTO, сообщение→DTO с цитатой reply); `GroupChatService` (10 методов: create/get-my-groups/get-group-by-id[+read]/add/remove/promote/leave[+авто-передача админства]/update-info/send-message/delete-message) с ролями Admin/Member, блок-проверками и служебными (System) сообщениями; SignalR `GroupChatHub` + `IGroupChatNotifier`/`GroupChatNotifier` (рассылка участникам, в т.ч. служебных); 3 валидатора; `GroupChatController` (10 эндпоинтов); DI + маппинг `/groupChatHub`; сид группы «Команда проекта» (admin-Admin + alice/bob-Member, служебные + текстовые + reply).
- **Следующий шаг:** начать Phase 16 — реакции/reply/forward/голосовые для личных и групповых: `MessageReaction` + enum `MessageContext {Direct, Group}`, поля `ReplyToMessageId`/`IsForwarded`/`MessageType`/`Duration`/`Waveform` в личном `Message`, эндпоинты `/Message/react`, `/Message/forward`, голосовые в `wwwroot/voice`, миграция.
- **Состояние сборки:** 🟢 зелёная (0 предупреждений; база 86 эндпоинтов: 76 + 10 `/GroupChat/*`). Проверено: сборка, offline-скрипт миграции (3 таблицы, FK каскадом, self-ref reply `ON DELETE SET NULL`, unique-индекс участника — SQL корректен), boot приложения (БД недоступна → ошибка миграции поймана, хост поднялся) + регистрация всех 10 `/GroupChat/*` в Swagger и маппинг `/groupChatHub`. Живой smoke-тест на PostgreSQL в эту сессию не гонялся (нет доступа к `DATABASE_URL`), запускается при деплое.

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

## Phase 16 — Сообщения: реакции, reply, forward, голосовые (личные + группы) · §8
- [ ] Изменения `Message`: `ReplyToMessageId?`, `IsForwarded`, `MessageType(Text/Image/File/Voice)`, `Duration?`, `Waveform?`
- [ ] Enum `MessageType`; enum `MessageContext {Direct, Group}`
- [ ] Сущность `MessageReaction { Id, MessageId, MessageContext, UserId, Emoji, CreatedAt }` — один юзер = одна реакция (повтор снимает, другой эмодзи заменяет)
- [ ] `POST /Message/react?messageId&context&emoji` (тумблер/замена) + real-time пуш
- [ ] Reply: цитата исходного в DTO (текст/превью)
- [ ] Forward: `POST /Message/forward?messageId&context&targetChatId&targetContext` — копия содержимого, `IsForwarded=true`
- [ ] Голосовые: `MessageType=Voice`, файл в `wwwroot/voice`, `Duration`, `Waveform` (JSON-массив; при недоступности генерации — плейсхолдер); отправка через send-message (или `send-voice`)
- [ ] Миграция

## Phase 17 — Сторис: close friends, ответы, репост поста · §9
- [ ] Сущность `CloseFriend { Id, UserId, FriendUserId, CreatedAt }`; `Story.Audience (enum All/CloseFriends)`, `Story.SharedPostId (int?)`
- [ ] Эндпоинты CloseFriend: `POST /add?userId`, `DELETE /remove?userId`, `GET /get-list`
- [ ] Публикация сторис с `audience`; в `get-stories`/`get-user-stories` фильтровать close-friends по членству зрителя
- [ ] Ответы на сторис: `POST /Story/reply?storyId` → личное сообщение автору (создать чат если нет) + сущность `StoryReply { Id, StoryId, FromUserId, MessageId, CreatedAt }` + уведомление `StoryReply`; учитывать `WhoCanReplyStory`
- [ ] Репост поста: `POST /Story/share-post?postId` → сторис с `SharedPostId` (только публичные посты) + уведомление `PostShared`
- [ ] Миграция

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
