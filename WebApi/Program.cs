using Infrastructure;
using Infrastructure.Data.Seed;
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
    try
    {
        await DbInitializer.InitializeAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Не удалось применить миграции/Seed при старте (БД недоступна?)");
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

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<GroupChatHub>("/groupChatHub");
app.MapHub<PresenceHub>("/presenceHub");

app.Run();
