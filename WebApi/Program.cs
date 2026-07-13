using Infrastructure;
using Infrastructure.Data.Seed;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Сервисы ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Слой данных: DbContext (PostgreSQL) + ядро Identity.
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Instagram Clone API",
        Version = "v1",
        Description = "Production-ready бэкенд Instagram-клона (ASP.NET Core 8 + PostgreSQL)."
    });
});

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

app.UseAuthorization();

app.MapControllers();

app.Run();
