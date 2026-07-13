# CLAUDE.md — Instagram Clone Backend

## Что это
Production-ready бэкенд Instagram-клона на **C# / ASP.NET Core 8 + PostgreSQL**.
ТЗ — источники истины:
- **База (Phase 0–10, готово):** [`instagram-backend-prompt.md`](./instagram-backend-prompt.md).
- **Новые фичи (Phase 11+, текущая работа):** [`instagram-backend-features-prompt.md`](./instagram-backend-features-prompt.md) — уведомления, приватность/блокировки, хэштеги/упоминания, ответы+лайки комментов, групповые чаты, реакции/reply/forward/voice, close friends/ответы/репост сторис, presence/typing, верификация, 2FA, Explore.

Строим точно по контракту API (пути, методы, параметры, DTO воспроизводить дословно). Существующий контракт базы не ломаем — только расширяем необязательными полями.

## Стек
ASP.NET Core 8 Web API · EF Core 8 + Npgsql · ASP.NET Core Identity (`IdentityUser<string>`) ·
JWT Bearer · AutoMapper · FluentValidation · Swashbuckle (Swagger) · SignalR (чат).

## Архитектура (слоистая)
- **Domain** — Entities, DTOs, Enums, Responses (`Response<T>`, `PagedResponse<T>`)
- **Infrastructure** — DataContext, EF-конфигурации, Services/Repositories, Migrations, Seed
- **WebApi** — Controllers, `Program.cs`, DI, SignalR Hub
- Файлы (картинки постов, аватары, файлы сообщений, сторис) → `wwwroot/images`, в БД только имя файла.

## Ключевые правила
- Все эндпоинты авторизованы по умолчанию, кроме `/Account/register` и `/Account/login`.
- **ID текущего юзера — из JWT claims, а не из параметров.**
- Единый формат ответа `Response<T>`; пагинация через `PagedResponse<T>`.
- Владелец ресурса: нельзя удалять чужие посты/комменты/сторис/сообщения.
- Загрузка файлов: проверка расширения/размера, уникальные Guid-имена, удаление файла с диска при удалении сущности.
- Сохранять оригинальные имена/опечатки контракта (напр. `massageId`).

## Рабочий процесс (важно)
Проект ведётся **сессиями** и управляется двумя командами:
- **`/start`** — читает [`ROADMAP.md`](./ROADMAP.md) и последние логи в [`.claude/sessions/`](./.claude/sessions/),
  продолжает следующие задачи, обновляет роадмап, затем **коммитит** и пишет лог сессии.
- **`/stop`** — коммитит незакоммиченные изменения и сохраняет лог текущей сессии.

Всегда:
1. Держать сборку зелёной (`dotnet build`).
2. Обновлять чекбоксы и блок «Текущий статус» в `ROADMAP.md`.
3. Писать лог сессии по шаблону из [`.claude/sessions/README.md`](./.claude/sessions/README.md).

## Команды
```bash
dotnet build
dotnet run --project WebApi
dotnet ef migrations add <Name> --project Infrastructure --startup-project WebApi
dotnet ef database update --project Infrastructure --startup-project WebApi
```
