# ROADMAP — Instagram Clone Backend (ASP.NET Core 8 + PostgreSQL)

Полное ТЗ: [`instagram-backend-prompt.md`](./instagram-backend-prompt.md).
Проект ведётся сессиями: `/start` продолжает работу, `/stop` фиксирует изменения.
Логи сессий: [`.claude/sessions/`](./.claude/sessions/).

## Легенда статусов
- `[ ]` — не начато
- `[~]` — в работе
- `[x]` — готово

## 📍 Текущий статус
- **Активная фаза:** Phase 10 — Качество, харденинг и документация ✅ **ЗАВЕРШЕНА** (все 7 пунктов). Проект готов к деплою.
- **Последняя сессия:** 2026-07-13 (13)
- **Следующий шаг:** опционально — деплой web service на Render (`Dockerfile` + `render.yaml` + env-переменные `DATABASE_URL`/`Jwt`). Дальше — доп. фичи сверх ТЗ по запросу.
- **Состояние сборки:** 🟢 зелёная (0 warnings, 0 errors). **Smoke-тест на живом PostgreSQL (Render, Ohio) пройден — 12/12 PASS**: login/register, get-my-profile, get-users (paged), get-posts (paged, счётчики), like-post (toggle), add-comment, get-post-by-id, get-Locations (paged, 5 seed), get-subscriptions, create-chat, no-token→401. Авто-миграция + seed на чистой БД Render отработали (admin/alice/bob/carol + профили, подписки, посты, локации). **Найден и исправлен реальный баг:** `User : IdentityUser<string>` не генерировал строковый `Id` (generic-конструктор его не задаёт, EF строковый ключ не автогенерит) → падали seed и регистрация с «primary key property 'Id' is null». Фикс — конструктор `User()` задаёт `Id`/`SecurityStamp` (как не-generic `IdentityUser`). Также добавлена поддержка `DATABASE_URL` (URL PaaS → Npgsql + `SSL Mode=Require`) в `DependencyInjection`. Статический аудит (сессия 12) ранее подтвердил: авторизация владельца по всем ресурсам, пагинация, валидаторы, глобальная обработка ошибок; README = полный список 57 эндпоинтов.

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

- [x] Enum `Gender { Male = 0, Female = 1 }`
- [x] Обёртки `Response<T>`, `PagedResponse<T>`
- [x] Сущности: User (`: IdentityUser<string>`), UserProfile
- [x] Сущности постов: Post, PostImage, PostLike, PostView, PostComment, PostFavorite
- [x] Сущности сторис: Story, StoryLike, StoryView
- [x] Прочие: FollowingRelationShip, Chat, Message, Location, SearchHistory, UserSearchHistory
- [x] DTO из контракта: RegisterDto, LoginDto, AddLocationDto, UpdateLocationDto, AddPostCommentDto, AddPostFavoriteDto, UpdateUserProfileDto, GetStoryDto, ViewerDto, GetStoryViewDto
- [x] Read/response DTO под ключевые эндпоинты (Get*Dto для Post/UserProfile/User/Location/Chat/Story; уточняются в фазах фич)

## Phase 2 — Слой данных (DbContext, EF-конфигурации, миграции, Seed)
> Цель: БД строится, наполняется тестовыми данными.

- [x] `DataContext : IdentityDbContext<User, IdentityRole, string>`
- [x] Fluent API: связи, каскады, индексы, уникальные ограничения
- [x] Начальная миграция (`InitialCreate`)
- [x] Seed: роли Admin/User + тестовые пользователи и данные (`DbInitializer`)
- [x] Авто-применение миграций и Seed при старте

## Phase 3 — Сквозная инфраструктура
> Цель: общие сервисы, которыми пользуются все фичи.

- [x] JWT token service + конфигурация (claims: userId, userName, email, role)
- [x] Глобальный middleware обработки исключений → `Response<T>` со statusCode/errors
- [x] File storage service (сохранение/удаление, проверка расширения/размера, Guid-имена)
- [x] Базовые профили AutoMapper
- [x] Подключение FluentValidation + базовые валидаторы
- [x] Swagger с Bearer-кнопкой, XML-комментариями, группировкой по тегам
- [x] Доступ к текущему юзеру из claims (не из параметров)
- [x] CORS AllowAll (dev)

## Phase 4 — Аутентификация и Account
> Цель: регистрация/вход/пароли работают, выдаётся JWT.

- [x] Настройка ASP.NET Core Identity
- [x] Account service + controller: `register`, `login`, `ForgotPassword`, `ResetPassword`, `ChangePassword`
- [x] Валидаторы Register/Login (совпадение паролей, уникальность email/userName)
- [x] При регистрации создаётся пустой UserProfile

## Phase 5 — Пользователи, профили, подписки
> Цель: соц-граф и профили.

- [x] User: `get-users` (поиск+пагинация), история поиска (текст и профили), `delete-user` (только Admin)
- [x] UserProfile: by-id со счётчиками и isFollowing, `get-my-profile`, `update`, image update/delete, `is-follow`, `get-post-favorites`
- [x] FollowingRelationShip: subscribers, subscriptions, add, delete (запрет дубля и подписки на себя)

## Phase 6 — Посты и взаимодействия
> Цель: посты, лента, лайки/комменты/просмотры/избранное.

- [x] CRUD: `add-post` (multipart, images required), `delete-post` (только автор), `get-post-by-id`, `get-my-posts`
- [x] Ленты: `get-posts` (фильтр+пагинация, счётчики, isLiked/isFavorite), `get-reels`, `get-following-post`
- [x] `like-post` (тумблер), `view-post` (уникально), `add-comment`/`delete-comment` (только автор), `add-post-favorite` (тумблер)

## Phase 7 — Сторис
> Цель: сторис с жизнью 24ч, лайки/просмотры/вьюеры.

- [x] `AddStories` (из поста или файла), `DeleteStory` (только автор)
- [x] `get-stories` (активные <24ч, сгруппированы по авторам), `get-user-stories/{userId}`, `get-my-stories`
- [x] `LikeStory`, `GetStoryById` (viewerDto), `add-story-view` (уникально на юзера)

## Phase 8 — Чат и SignalR
> Цель: чаты и сообщения в реальном времени.

- [x] Chat service + controller: `get-chats` (последнее сообщение + непрочитанные), `get-chat-by-id` (пометить прочитанным), `create-chat` (дедуп)
- [x] `send-message` (multipart), `delete-message` (только отправитель, сохранить опечатку `massageId`), `delete-chat` (участник)
- [x] SignalR `ChatHub`: доставка/отправка сообщений в реальном времени

## Phase 9 — Локации и поиск
> Цель: справочник локаций (независимая фича).

- [x] Location CRUD + фильтр/пагинация (`get-Locations`, `get-Location-by-id`, `add`, `update`, `delete`)
- [x] Проверить, что все search-history эндпоинты User завершены (Phase 5)

## Phase 10 — Качество, харденинг и документация
> Цель: production-ready, всё проверено и задокументировано.

- [x] Аудит авторизации владельца по всем ресурсам (посты/комменты/сторис/сообщения)
- [x] Аудит пагинации везде, где есть PageNumber/PageSize
- [x] Ревизия сообщений валидации
- [x] Проверка глобальной обработки ошибок
- [x] Полный README (setup, миграции, connection string, `dotnet run`, список эндпоинтов)
- [x] Ручной smoke-тест всех групп эндпоинтов (на живом PostgreSQL Render — 12/12 PASS)
- [x] Финальная сборка без предупреждений
