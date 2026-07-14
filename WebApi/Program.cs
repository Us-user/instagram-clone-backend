using Infrastructure;
using Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using WebApi.Extensions;
using WebApi.Hubs;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// PaaS-хостинги (Render/Heroku/Railway) передают назначенный порт через переменную
// окружения PORT. Слушаем её на всех интерфейсах (0.0.0.0), иначе контейнер не пройдёт
// health-check. Локально переменной нет — работает обычный Kestrel (5000/5001).
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ── Сервисы ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// Слой данных + сквозные сервисы (DbContext, Identity, токены, файлы, текущий юзер,
// AutoMapper, FluentValidation).
builder.Services.AddInfrastructure(builder.Configuration);

// SignalR (чат в реальном времени). Реализация IChatNotifier поверх ChatHub живёт здесь,
// в WebApi; сервис чата в Infrastructure зависит только от абстракции.
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
builder.Services.AddScoped<IChatNotifier, ChatNotifier>();
builder.Services.AddScoped<INotificationNotifier, NotificationNotifier>();
builder.Services.AddScoped<IGroupChatNotifier, GroupChatNotifier>();
builder.Services.AddScoped<IPresenceNotifier, PresenceNotifier>();
builder.Services.AddScoped<ITypingNotifier, TypingNotifier>();

// JWT-аутентификация + авторизация (все эндпоинты защищены по умолчанию).
builder.Services.AddJwtAuthentication(builder.Configuration);

// Swagger с кнопкой Bearer-авторизации и XML-комментариями.
builder.Services.AddSwaggerWithBearer();

// CORS: для разработки разрешаем всё (правило из ТЗ).
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// ── Авто-применение миграций и Seed при старте ────────────────────────────────
// Если БД недоступна — логируем и продолжаем, чтобы приложение (и Swagger) поднялось.
using (var scope = app.Services.CreateScope())
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    // На первом деплое (Render Blueprint и т.п.) веб-сервис может стартовать раньше, чем БД
    // допровизионится/примет подключения. Ретраим применение миграций/Seed с бэкоффом, иначе
    // приложение осталось бы без схемы и все запросы к БД возвращали бы 500.
    const int maxAttempts = 10;
    var delay = TimeSpan.FromSeconds(3);
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await DbInitializer.InitializeAsync(scope.ServiceProvider);
            logger.LogInformation("Миграции/Seed применены (попытка {Attempt}).", attempt);
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex,
                "БД недоступна (попытка {Attempt}/{Max}), повтор через {Delay}с…",
                attempt, maxAttempts, delay.TotalSeconds);
            await Task.Delay(delay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Не удалось применить миграции/Seed после {Max} попыток — БД недоступна?", maxAttempts);
        }
    }
}

// ── Конвейер обработки запросов ───────────────────────────────────────────────
// Глобальная обработка исключений — первым, чтобы ловить ошибки всего конвейера.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger доступен во всех окружениях: это учебный/демо-API, и живой smoke-тест после
// деплоя удобнее всего гонять через Swagger UI. Открывается на корне (/) и на /swagger.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Instagram Clone API v1");
    options.RoutePrefix = "swagger";
});

// Раздача загруженных файлов из wwwroot (картинки постов, аватары, сторис, файлы сообщений).
app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Health-check для Render (и корень с указателем на Swagger). Анонимные, лёгкие.
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();

// ВРЕМЕННАЯ диагностика подключения к БД/состояния миграций (удалить после проверки деплоя).
app.MapGet("/health/db", async (Infrastructure.Data.DataContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        return Results.Ok(new { canConnect, appliedCount = applied.Count, pendingCount = pending.Count, pending });
    }
    catch (Exception ex)
    {
        return Results.Json(
            new { error = ex.GetType().Name, message = ex.Message, inner = ex.InnerException?.Message },
            statusCode: 500);
    }
}).AllowAnonymous();

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<GroupChatHub>("/groupChatHub");
app.MapHub<PresenceHub>("/presenceHub");

app.Run();
