# ROADMAP — Instagram Clone Backend (ASP.NET Core 8 + PostgreSQL)

Полное ТЗ: [`instagram-backend-prompt.md`](./instagram-backend-prompt.md).
Проект ведётся сессиями: `/start` продолжает работу, `/stop` фиксирует изменения.
Логи сессий: [`.claude/sessions/`](./.claude/sessions/).

## Легенда статусов
- `[ ]` — не начато
- `[~]` — в работе
- `[x]` — готово

## 📍 Текущий статус
- **Активная фаза:** Phase 1 — Доменная модель
- **Последняя сессия:** 2026-07-13 (02)
- **Следующий шаг:** Phase 1 — Enum `Gender`, `Response<T>`/`PagedResponse<T>`, сущности и DTO из контракта
- **Состояние сборки:** 🟢 зелёная (0 warnings, 0 errors); приложение запускается, Swagger отвечает (проверено)

---

## Phase 0 — Фундамент и инструментарий
> Цель: компилируемое пустое решение со слоистой архитектурой, которое запускается.

- [x] Создать solution `InstagramClone.sln`
- [x] Проекты: `Domain` (classlib), `Infrastructure` (classlib), `WebApi` (web)
- [x] Ссылки: Infrastructure → Domain, WebApi → Infrastructure & Domain
- [x] NuGet: EF Core 8, Npgsql, Identity, JWT Bearer, AutoMapper, FluentValidation, Swashbuckle, SignalR
- [x] `appsettings.json` с плейсхолдерами (ConnectionString PostgreSQL, Jwt: Issuer/Audience/Key/Lifetime)
- [x] Минимальный `Program.cs`, который запускается
- [x] `wwwroot/images` + отдача статики
- [x] Скелет README

## Phase 1 — Доменная модель (Entities, Enums, DTO, Responses)
> Цель: все доменные типы компилируются.

- [ ] Enum `Gender { Male = 0, Female = 1 }`
- [ ] Обёртки `Response<T>`, `PagedResponse<T>`
- [ ] Сущности: User (`: IdentityUser<string>`), UserProfile
- [ ] Сущности постов: Post, PostImage, PostLike, PostView, PostComment, PostFavorite
- [ ] Сущности сторис: Story, StoryLike, StoryView
- [ ] Прочие: FollowingRelationShip, Chat, Message, Location, SearchHistory, UserSearchHistory
- [ ] DTO из контракта: RegisterDto, LoginDto, AddLocationDto, UpdateLocationDto, AddPostCommentDto, AddPostFavoriteDto, UpdateUserProfileDto, GetStoryDto, ViewerDto, GetStoryViewDto
- [ ] Read/response DTO под каждый эндпоинт

## Phase 2 — Слой данных (DbContext, EF-конфигурации, миграции, Seed)
> Цель: БД строится, наполняется тестовыми данными.

- [ ] `DataContext : IdentityDbContext<User, IdentityRole, string>`
- [ ] Fluent API: связи, каскады, индексы, уникальные ограничения
- [ ] Начальная миграция
- [ ] Seed: роли Admin/User + тестовые пользователи и данные
- [ ] Авто-применение миграций и Seed при старте

## Phase 3 — Сквозная инфраструктура
> Цель: общие сервисы, которыми пользуются все фичи.

- [ ] JWT token service + конфигурация (claims: userId, userName, email, role)
- [ ] Глобальный middleware обработки исключений → `Response<T>` со statusCode/errors
- [ ] File storage service (сохранение/удаление, проверка расширения/размера, Guid-имена)
- [ ] Базовые профили AutoMapper
- [ ] Подключение FluentValidation + базовые валидаторы
- [ ] Swagger с Bearer-кнопкой, XML-комментариями, группировкой по тегам
- [ ] Доступ к текущему юзеру из claims (не из параметров)
- [ ] CORS AllowAll (dev)

## Phase 4 — Аутентификация и Account
> Цель: регистрация/вход/пароли работают, выдаётся JWT.

- [ ] Настройка ASP.NET Core Identity
- [ ] Account service + controller: `register`, `login`, `ForgotPassword`, `ResetPassword`, `ChangePassword`
- [ ] Валидаторы Register/Login (совпадение паролей, уникальность email/userName)
- [ ] При регистрации создаётся пустой UserProfile

## Phase 5 — Пользователи, профили, подписки
> Цель: соц-граф и профили.

- [ ] User: `get-users` (поиск+пагинация), история поиска (текст и профили), `delete-user` (только Admin)
- [ ] UserProfile: by-id со счётчиками и isFollowing, `get-my-profile`, `update`, image update/delete, `is-follow`, `get-post-favorites`
- [ ] FollowingRelationShip: subscribers, subscriptions, add, delete (запрет дубля и подписки на себя)

## Phase 6 — Посты и взаимодействия
> Цель: посты, лента, лайки/комменты/просмотры/избранное.

- [ ] CRUD: `add-post` (multipart, images required), `delete-post` (только автор), `get-post-by-id`, `get-my-posts`
- [ ] Ленты: `get-posts` (фильтр+пагинация, счётчики, isLiked/isFavorite), `get-reels`, `get-following-post`
- [ ] `like-post` (тумблер), `view-post` (уникально), `add-comment`/`delete-comment` (только автор), `add-post-favorite` (тумблер)

## Phase 7 — Сторис
> Цель: сторис с жизнью 24ч, лайки/просмотры/вьюеры.

- [ ] `AddStories` (из поста или файла), `DeleteStory` (только автор)
- [ ] `get-stories` (активные <24ч, сгруппированы по авторам), `get-user-stories/{userId}`, `get-my-stories`
- [ ] `LikeStory`, `GetStoryById` (viewerDto), `add-story-view` (уникально на юзера)

## Phase 8 — Чат и SignalR
> Цель: чаты и сообщения в реальном времени.

- [ ] Chat service + controller: `get-chats` (последнее сообщение + непрочитанные), `get-chat-by-id` (пометить прочитанным), `create-chat` (дедуп)
- [ ] `send-message` (multipart), `delete-message` (только отправитель, сохранить опечатку `massageId`), `delete-chat` (участник)
- [ ] SignalR `ChatHub`: доставка/отправка сообщений в реальном времени

## Phase 9 — Локации и поиск
> Цель: справочник локаций (независимая фича).

- [ ] Location CRUD + фильтр/пагинация (`get-Locations`, `get-Location-by-id`, `add`, `update`, `delete`)
- [ ] Проверить, что все search-history эндпоинты User завершены (Phase 5)

## Phase 10 — Качество, харденинг и документация
> Цель: production-ready, всё проверено и задокументировано.

- [ ] Аудит авторизации владельца по всем ресурсам (посты/комменты/сторис/сообщения)
- [ ] Аудит пагинации везде, где есть PageNumber/PageSize
- [ ] Ревизия сообщений валидации
- [ ] Проверка глобальной обработки ошибок
- [ ] Полный README (setup, миграции, connection string, `dotnet run`, список эндпоинтов)
- [ ] Ручной smoke-тест всех групп эндпоинтов
- [ ] Финальная сборка без предупреждений
