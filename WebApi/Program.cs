using Infrastructure;
using Infrastructure.Data.Seed;
using WebApi.Extensions;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Сервисы ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// Слой данных + сквозные сервисы (DbContext, Identity, токены, файлы, текущий юзер,
// AutoMapper, FluentValidation).
builder.Services.AddInfrastructure(builder.Configuration);

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Instagram Clone API v1");
    });
}

// Раздача загруженных файлов из wwwroot (картинки постов, аватары, сторис, файлы сообщений).
app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
