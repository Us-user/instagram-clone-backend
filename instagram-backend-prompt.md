# Промпт: Полный бэкенд Instagram-клона (C# / ASP.NET Core + PostgreSQL)

> Скопируй всё, что ниже разделителя, и передай модели/инструменту для генерации кода.
> Основано на контракте API: `https://instagram-api.softclub.tj/swagger/v1/swagger.json`

---

Ты — senior .NET backend разработчик. Собери с нуля полный production-ready бэкенд Instagram-клона на **C# / ASP.NET Core (.NET 8) + PostgreSQL**, точно повторяющий контракт API ниже. Некоторые эндпоинты в оригинале не работают — ты должен реализовать **всю бизнес-логику полностью и корректно**.

## Стек и архитектура

- ASP.NET Core 8 Web API
- Entity Framework Core 8 + Npgsql (PostgreSQL)
- ASP.NET Core Identity (`IdentityUser<string>` с расширением)
- JWT Bearer аутентификация
- AutoMapper для маппинга Entity ↔ DTO
- FluentValidation для валидации DTO
- Swashbuckle (Swagger) с настроенной кнопкой Bearer-авторизации
- Слоистая архитектура: **Domain** (Entities, DTOs, Enums, Responses), **Infrastructure** (DataContext, Services/Repositories, Migrations, Seed), **WebApi** (Controllers, Program.cs, DI)
- Файлы (изображения постов, аватары, файлы сообщений, сторис) хранить в `wwwroot/images`, раздавать как статику; в БД хранить только имя файла
- SignalR для чата в реальном времени (отправка/доставка сообщений)

## Единый формат ответа

Все эндпоинты возвращают обёртку:

```csharp
Response<T> { T data; List<string> errors; int statusCode; }
```

Плюс `PagedResponse<T>` с полями `totalRecords`, `pageNumber`, `pageSize` для списков с пагинацией.

## Аутентификация

JWT Bearer. В токен класть claims: `userId`, `userName`, `email`, роль. Все эндпоинты требуют авторизации по умолчанию, кроме `/Account/register` и `/Account/login` (`[AllowAnonymous]`). **ID текущего юзера брать из claims, а не из параметров.**

## Сущности БД (PostgreSQL, EF Core)

| Сущность | Поля |
|---|---|
| **User** (`: IdentityUser<string>`) | + FullName, Avatar (nullable) + навигации |
| **UserProfile** | Id, UserId (FK 1:1), About (nullable), Gender (enum 0=Male, 1=Female), Image (nullable) |
| **Post** | Id, UserId (FK), Title (nullable), Content (nullable), CreatedAt, IsReel (bool); навигации Images, Likes, Comments, Views, Favorites |
| **PostImage** | Id, PostId (FK), ImageName |
| **PostLike** | Id, PostId (FK), UserId (FK), CreatedAt |
| **PostView** | Id, PostId (FK), UserId (FK) |
| **PostComment** | Id, PostId (FK), UserId (FK), Comment, CreatedAt |
| **PostFavorite** | Id, PostId (FK), UserId (FK), CreatedAt |
| **Story** | Id, UserId (FK), FileName (nullable), PostId (nullable FK), CreatedAt (живёт 24 часа) |
| **StoryLike** | Id, StoryId (FK), UserId (FK) |
| **StoryView** | Id, StoryId (FK), ViewUserId (FK) |
| **FollowingRelationShip** | Id, UserId (FK — подписчик), FollowingUserId (FK — на кого), CreatedAt |
| **Chat** | Id, User1Id (FK), User2Id (FK), CreatedAt |
| **Message** | Id, ChatId (FK), SenderUserId (FK), MessageText (nullable), FileName (nullable), CreatedAt, IsRead |
| **Location** | Id, City, State, ZipCode, Country |
| **SearchHistory** | Id, UserId (FK), Text, CreatedAt |
| **UserSearchHistory** | Id, UserId (FK), SearchedUserId (FK), CreatedAt |

Настрой все связи, каскады, индексы через Fluent API. Создай миграции и Seed (роли Admin/User + тестовые данные).

## Эндпоинты (пути, методы, параметры воспроизвести ТОЧНО)

### Account
- `POST /Account/register` — body `RegisterDto{userName, fullName, email, password, confirmPassword}`. Проверить совпадение паролей, уникальность email/userName; создать User + пустой UserProfile.
- `POST /Account/login` — body `LoginDto{userName, password}`. Вернуть JWT в `data`.
- `DELETE /Account/ForgotPassword` — query `Email`. Сгенерировать reset-токен (в учебных целях вернуть его в ответе/лог).
- `DELETE /Account/ResetPassword` — query `Token, Email, Password, ConfirmPassword`. Сбросить пароль по токену.
- `PUT /Account/ChangePassword` — query `OldPassword, Password, ConfirmPassword`. Для текущего юзера.

### FollowingRelationShip
- `GET /FollowingRelationShip/get-subscribers?UserId` — кто подписан на UserId (followers).
- `GET /FollowingRelationShip/get-subscriptions?UserId` — на кого подписан UserId (following).
- `POST /FollowingRelationShip/add-following-relation-ship?followingUserId` — текущий юзер подписывается. Запретить дубль и подписку на себя.
- `DELETE /FollowingRelationShip/delete-following-relation-ship?followingUserId` — отписка.

### Post
- `GET /Post/get-posts?UserId&Title&Content&PageNumber&PageSize` — фильтрация + пагинация. В каждом посте вернуть счётчики лайков/комментов/просмотров и флаг isLiked/isFavorite для текущего юзера.
- `GET /Post/get-reels?PageNumber&PageSize` — только посты с IsReel = true.
- `GET /Post/get-post-by-id?id`
- `GET /Post/get-my-posts` — посты текущего юзера.
- `GET /Post/get-following-post?UserId&PageNumber&PageSize` — лента из постов тех, на кого подписан.
- `POST /Post/add-post` — `multipart/form-data`: Title, Content, Images (массив файлов, required). Сохранить файлы, создать Post + PostImage.
- `DELETE /Post/delete-post?id` — только автор.
- `POST /Post/like-post?postId` — тумблер лайка (лайк/снять).
- `POST /Post/view-post?postId` — зафиксировать просмотр (уникально на юзера).
- `POST /Post/add-comment` — body `AddPostCommentDto{comment, postId}`.
- `DELETE /Post/delete-comment?commentId` — только автор коммента.
- `POST /Post/add-post-favorite` — body `AddPostFavoriteDto{postId}` — тумблер избранного.

### Story
- `GET /Story/get-stories` — активные (< 24ч) сторис тех, на кого подписан текущий юзер, сгруппированные по авторам.
- `GET /Story/get-user-stories/{userId}` — сторис конкретного юзера.
- `GET /Story/get-my-stories`
- `POST /Story/LikeStory?storyId` → `Response<string>`.
- `GET /Story/GetStoryById?id` → `Response<GetStoryDto>`. `GetStoryDto{id, fileName, postId, createAt, userId, userAvatar, viewerDto{userName, name, viewCount, viewLike}}`.
- `POST /Story/AddStories?PostId` + `multipart/form-data` Image. PostId опционален (сторис из поста или из файла).
- `DELETE /Story/DeleteStory?id` → `Response<bool>`, только автор.
- `POST /Story/add-story-view?StoryId` → `Response<GetStoryViewDto>`. `GetStoryViewDto{id, viewUserId, storyId}`. Уникально на юзера.

### Chat (+ SignalR)
- `GET /Chat/get-chats` — чаты текущего юзера + последнее сообщение + непрочитанные.
- `GET /Chat/get-chat-by-id?chatId` — чат + все сообщения (пометить прочитанными).
- `POST /Chat/create-chat?receiverUserId` — создать/вернуть существующий чат между текущим и receiver.
- `PUT /Chat/send-message` — `multipart/form-data`: ChatId (required), MessageText, File. Разослать через SignalR.
- `DELETE /Chat/delete-message?massageId` — (в оригинале опечатка `massageId` — сохранить как есть), только отправитель.
- `DELETE /Chat/delete-chat?chatId` — участник чата.

### Location
- `GET /Location/get-Locations?City&State&ZipCode&Country&PageNumber&PageSize` — фильтр + пагинация.
- `GET /Location/get-Location-by-id?id`
- `POST /Location/add-Location` — body `AddLocationDto{city, state, zipCode, country}` (все required).
- `PUT /Location/update-Location` — body `UpdateLocationDto{locationId, city, state, zipCode, country}`.
- `DELETE /Location/delete-Location?id`

### User
- `GET /User/get-users?UserName&Email&PageNumber&PageSize` — поиск + пагинация.
- `POST /User/add-search-history?Text`
- `GET /User/get-search-histories`
- `DELETE /User/delete-search-history?id`
- `DELETE /User/delete-search-histories` — очистить всё.
- `POST /User/add-user-search-history?UserSearchId` — записать «просмотрел профиль».
- `GET /User/get-user-search-histories`
- `DELETE /User/delete-user-search-history?id`
- `DELETE /User/delete-user-search-histories`
- `DELETE /User/delete-user?userId` — только Admin.

### UserProfile
- `GET /UserProfile/get-user-profile-by-id?id` — профиль + счётчики постов/подписчиков/подписок + isFollowing.
- `GET /UserProfile/get-is-follow-user-profile-by-id?followingUserId` — bool подписан ли текущий.
- `GET /UserProfile/get-my-profile`
- `PUT /UserProfile/update-user-profile` — body `UpdateUserProfileDto{about, gender}`.
- `GET /UserProfile/get-post-favorites?PageNumber&PageSize` — избранные посты текущего.
- `PUT /UserProfile/update-user-image-profile` — `multipart/form-data` imageFile.
- `DELETE /UserProfile/delete-user-image-profile`

## DTO из контракта (воспроизвести точно)

```csharp
RegisterDto     { string userName; string fullName; string email; string password; string confirmPassword; }        // все required
LoginDto        { string userName; string password; }                                                                 // все required
AddLocationDto  { string city; string state; string zipCode; string country; }                                        // все required
UpdateLocationDto { int locationId; string city; string state; string zipCode; string country; }                      // все required
AddPostCommentDto { string comment; int postId; }                                                                     // required
AddPostFavoriteDto { int postId; }                                                                                    // required
UpdateUserProfileDto { string about; Gender gender; }                                                                 // Gender enum {Male=0, Female=1}
GetStoryDto     { int id; string fileName; int? postId; DateTime createAt; string userId; string userAvatar; ViewerDto viewerDto; }
ViewerDto       { string userName; string name; int? viewCount; int? viewLike; }
GetStoryViewDto { int id; string viewUserId; int storyId; }
```

## Требования к качеству

1. Все «нерабочие» эндпоинты оригинала реализовать полностью и рабоче.
2. Глобальный middleware обработки исключений → корректный `Response<T>` с `statusCode` и `errors`.
3. Валидация входных данных (FluentValidation) с понятными сообщениями.
4. Авторизация владельца ресурса (нельзя удалять чужие посты/комменты/сторис/сообщения).
5. Пагинация везде, где есть PageNumber/PageSize.
6. Загрузка файлов: проверка расширения/размера, уникальные имена (Guid), удаление файла с диска при удалении сущности.
7. Swagger с Bearer-авторизацией, XML-комментариями и сгруппированными по тегам эндпоинтами.
8. `appsettings.json`: строка подключения PostgreSQL, JWT (Issuer, Audience, Key, срок жизни).
9. `Program.cs`: подключить DbContext, Identity, JWT, AutoMapper, FluentValidation, SignalR, CORS (AllowAll для дев), статику `wwwroot`, авто-применение миграций и Seed при старте.
10. README: как запустить (миграции, connection string, `dotnet run`), список эндпоинтов.

## Что отдать в результате

Полную структуру решения со всеми файлами: сущности, DataContext + конфигурации, DTO, AutoMapper-профили, валидаторы, сервисы с бизнес-логикой, контроллеры, SignalR-хаб чата, `Program.cs`, `appsettings.json`, миграции, Seed, README. Код должен компилироваться и запускаться без доработок.
