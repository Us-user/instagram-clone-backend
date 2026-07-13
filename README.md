# Instagram Clone — Backend

Production-ready бэкенд Instagram-клона на **C# / ASP.NET Core 8 + PostgreSQL**.
Построен строго по контракту API из [`instagram-backend-prompt.md`](./instagram-backend-prompt.md)
(пути, методы, параметры и DTO воспроизведены дословно).

> Статус: **все фазы фич реализованы** (Account, User, UserProfile, подписки, посты, сторис, чат, локации).
> Идёт финальный харденинг и документирование. Прогресс и план — в [`ROADMAP.md`](./ROADMAP.md).

## Стек
ASP.NET Core 8 Web API · EF Core 8 + Npgsql · ASP.NET Core Identity (`IdentityUser<string>`) ·
JWT Bearer · AutoMapper · FluentValidation · Swashbuckle (Swagger) · SignalR (чат).

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
Swagger UI (в Development): `http://localhost:<port>/swagger` — есть кнопка **Authorize**
для передачи JWT (вставляется сам токен, без префикса `Bearer `).

## Миграции (EF Core)
Миграции и Seed применяются **автоматически при старте** приложения (`DbInitializer`) —
достаточно поднять PostgreSQL и запустить проект. Если БД недоступна, старт не падает
(ошибка логируется, Swagger всё равно поднимается). Вручную:
```bash
dotnet ef migrations add <Name> --project Infrastructure --startup-project WebApi
dotnet ef database update --project Infrastructure --startup-project WebApi
```

## Тестовые аккаунты (Seed)
При первом запуске создаются роли `Admin`/`User` и тестовые пользователи с профилями:

| Логин | Пароль | Роли |
|---|---|---|
| `admin` | `Admin123!` | Admin, User |
| `alice` | `User123!` | User |
| `bob`   | `User123!` | User |
| `carol` | `User123!` | User |

Также сидируются подписки, пара постов (с лайком и комментарием) и справочник локаций.

## Аутентификация и авторизация
- Аутентификация — **JWT Bearer**. Токен выдаётся `/Account/register` и `/Account/login` в поле `data`.
- **Все эндпоинты защищены по умолчанию** (fallback-политика `RequireAuthenticatedUser`).
  Открыты только `register`, `login`, `ForgotPassword`, `ResetPassword` (помечены `[AllowAnonymous]`).
- **Id текущего пользователя всегда берётся из JWT-claims**, а не из параметров запроса.
- Владелец ресурса: удалять чужие посты/комменты/сторис/сообщения нельзя (`403 Forbidden`);
  `delete-user` доступен только роли `Admin`.
- Claims в токене: `userId`, `userName`, `email`, `role`.

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
| POST | `login` 🔓 | body `LoginDto` |
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
| GET | `get-subscribers` | `?userId` |
| GET | `get-subscriptions` | `?userId` |
| POST | `add-following-relation-ship` | `?followingUserId` |
| DELETE | `delete-following-relation-ship` | `?followingUserId` |

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
| POST | `AddStories` | `?postId` + multipart `AddStoryDto` (Image) |
| DELETE | `DeleteStory` | `?id` (только автор) |
| POST | `add-story-view` | `?storyId` (уникально на юзера) |

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

## SignalR — чат в реальном времени
- Хаб: `/chatHub` (требует JWT).
- Токен передаётся query-параметром `access_token` (WebSocket не шлёт заголовок `Authorization`):
  `ws://localhost:<port>/chatHub?access_token=<JWT>`.
- При `PUT /Chat/send-message` сервер рассылает обоим участникам чата клиентский метод
  `ReceiveMessage(GetMessageDto)`.

## Рабочий процесс
Проект ведётся сессиями: `/start` продолжает работу по роадмапу, `/stop` фиксирует изменения.
Логи сессий — в [`.claude/sessions/`](./.claude/sessions/).
