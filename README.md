# Instagram Clone — Backend

Production-ready бэкенд Instagram-клона на **C# / ASP.NET Core 8 + PostgreSQL**.
Строится строго по контракту API из [`instagram-backend-prompt.md`](./instagram-backend-prompt.md).

> Статус: **в разработке**. Прогресс и план — в [`ROADMAP.md`](./ROADMAP.md).

## Стек
ASP.NET Core 8 Web API · EF Core 8 + Npgsql · ASP.NET Core Identity (`IdentityUser<string>`) ·
JWT Bearer · AutoMapper · FluentValidation · Swashbuckle (Swagger) · SignalR (чат).

## Архитектура (слоистая)
| Проект | Назначение |
|---|---|
| **Domain** | Entities, DTOs, Enums, Responses (`Response<T>`, `PagedResponse<T>`) |
| **Infrastructure** | DataContext, EF-конфигурации, сервисы/репозитории, миграции, seed |
| **WebApi** | Controllers, `Program.cs`, DI, SignalR Hub |

Загруженные файлы (картинки постов, аватары, файлы сообщений, сторис) хранятся в
`WebApi/wwwroot/images` и раздаются как статика; в БД сохраняется только имя файла.

## Требования
- .NET SDK 8+ (проект таргетит `net8.0`; собран и запускается также на рантайме .NET 10 — см. `RollForward` в `WebApi.csproj`).
- PostgreSQL 14+.

## Настройка
1. Задать строку подключения и параметры JWT в `WebApi/appsettings.json`
   (секция `ConnectionStrings:DefaultConnection` и `Jwt`).
   Для локальной разработки можно использовать `WebApi/appsettings.Development.json`
   (не коммитится).

## Запуск
```bash
dotnet build
dotnet run --project WebApi
```
Swagger UI (в Development): `https://localhost:<port>/swagger`.

## Миграции (EF Core)
```bash
dotnet ef migrations add <Name> --project Infrastructure --startup-project WebApi
dotnet ef database update --project Infrastructure --startup-project WebApi
```

## Эндпоинты
Полный список — в [`instagram-backend-prompt.md`](./instagram-backend-prompt.md)
(Account, User, UserProfile, FollowingRelationShip, Post, Story, Chat, Location).
Раздел будет дополняться по мере реализации.

## Рабочий процесс
Проект ведётся сессиями: `/start` продолжает работу по роадмапу, `/stop` фиксирует изменения.
Логи сессий — в [`.claude/sessions/`](./.claude/sessions/).
