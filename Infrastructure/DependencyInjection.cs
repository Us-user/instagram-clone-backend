using Domain.Entities;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Mapping;
using Infrastructure.Options;
using Infrastructure.Services;
using Infrastructure.Services.Interfaces;
using Infrastructure.Services.Streaming;
using Infrastructure.Validators.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Infrastructure;

/// <summary>
/// Регистрация сервисов слоя Infrastructure: DbContext, ядро Identity, сквозные сервисы
/// (токены, файлы, текущий пользователь), AutoMapper и FluentValidation-валидаторы.
/// Схема JWT-аутентификации подключается в WebApi (пакет JwtBearer живёт там).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<DataContext>(options =>
            options.UseNpgsql(ResolveConnectionString(configuration)));

        // AddIdentityCore не тянет cookie-схему аутентификации — это удобно для JWT-API.
        // Даёт UserManager/RoleManager (нужны для Seed и Account-сервиса).
        services.AddIdentityCore<User>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<DataContext>()
            .AddDefaultTokenProviders();

        // Параметры JWT из секции "Jwt".
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Доступ к HttpContext для чтения claim'ов текущего пользователя.
        services.AddHttpContextAccessor();

        // Сквозные сервисы.
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileService, FileService>();
        // Строит абсолютные URL картинок из имён файлов для необязательных *Url-полей DTO.
        services.AddScoped<IImageUrlBuilder, ImageUrlBuilder>();
        services.AddScoped<ITotpService, TotpService>();

        // Модуль сессий (access + refresh): управление сессиями, определение устройства и геолокация.
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IDeviceInfoService, DeviceInfoService>();
        services.AddSingleton<IGeoLocationService, GeoLocationService>();
        // Троттлинг записи LastActivityAt — эфемерное состояние в памяти (как presence/typing-трекеры).
        services.AddSingleton<ISessionActivityThrottle, SessionActivityThrottle>();
        // Фоновая очистка истёкших/давно отозванных сессий (раз в сутки).
        services.AddHostedService<SessionCleanupService>();

        // Сервисы фич.
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IFollowingRelationShipService, FollowingRelationShipService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IStoryService, StoryService>();
        services.AddScoped<ICloseFriendService, CloseFriendService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IBlockService, BlockService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IHashtagService, HashtagService>();
        services.AddScoped<IMentionService, MentionService>();
        services.AddScoped<IGroupChatService, GroupChatService>();
        services.AddScoped<IPresenceService, PresenceService>();
        services.AddScoped<ITypingService, TypingService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IExploreService, ExploreService>();

        // ── Модуль эфиров (Live Streaming) ────────────────────────────────────────────
        // Параметры провайдера + выбор реализации по конфигу. Провайдер LiveKit включается, только когда
        // выбран явно И заданы ключи/URL — иначе (в т.ч. локально) работает Fake-заглушка, чтобы бизнес-
        // логику эфиров можно было проверять без реального WebRTC. Валидатор вебхуков идёт в паре с провайдером.
        var streamingSection = configuration.GetSection(StreamingOptions.SectionName);
        services.Configure<StreamingOptions>(streamingSection);
        var streaming = streamingSection.Get<StreamingOptions>() ?? new StreamingOptions();
        var useLiveKit = string.Equals(streaming.Provider, "LiveKit", StringComparison.OrdinalIgnoreCase)
                         && !string.IsNullOrWhiteSpace(streaming.LiveKit.ApiKey)
                         && !string.IsNullOrWhiteSpace(streaming.LiveKit.ApiSecret)
                         && !string.IsNullOrWhiteSpace(streaming.LiveKit.Url);
        if (useLiveKit)
        {
            services.AddScoped<IStreamingProvider, LiveKitStreamingProvider>();
            services.AddScoped<ILiveWebhookValidator, LiveKitWebhookValidator>();
        }
        else
        {
            services.AddScoped<IStreamingProvider, FakeStreamingProvider>();
            services.AddScoped<ILiveWebhookValidator, FakeLiveWebhookValidator>();
        }

        // Сообщает при старте, какой провайдер активен: фолбэк на Fake иначе проходит незаметно.
        services.AddHostedService<StreamingStartupLogger>();

        services.AddScoped<ILiveStreamService, LiveStreamService>();
        // Эфемерное in-memory состояние эфиров (как presence/typing-трекеры): привязка соединений к эфиру
        // (грейс-период при обрыве) и троттлинг сердечек/комментариев.
        services.AddSingleton<ILiveConnectionTracker, LiveConnectionTracker>();
        services.AddSingleton<ILiveRateLimiter, LiveRateLimiter>();
        // Фоновое автозавершение «висящих» эфиров.
        services.AddHostedService<LiveStreamCleanupService>();

        // Presence/typing (§1): состояние присутствия и «печатает…» — эфемерное, живёт в памяти
        // между всеми соединениями, поэтому singleton (реализация SignalR-рассылки — в WebApi).
        services.AddSingleton<IPresenceTracker, PresenceTracker>();
        services.AddSingleton<ITypingTracker, TypingTracker>();

        // Login-флоу 2FA (§11): временные токены сессии и email-коды — эфемерное состояние в памяти
        // (переживать рестарт не нужно, TTL ~10 минут), как presence/typing-трекеры.
        services.AddSingleton<ITwoFactorTokenStore, TwoFactorTokenStore>();

        // AutoMapper: профили из этой сборки.
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        // FluentValidation: все валидаторы из этой сборки.
        services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

        return services;
    }

    /// <summary>
    /// Определяет строку подключения к PostgreSQL. Приоритет — переменной <c>DATABASE_URL</c>
    /// (формат PaaS-хостингов вроде Render/Heroku: <c>postgres://user:pass@host:port/db</c>),
    /// которая конвертируется в key-value формат Npgsql с включённым SSL. Если её нет —
    /// берётся обычная <c>ConnectionStrings:DefaultConnection</c> из конфигурации.
    /// </summary>
    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
            return BuildNpgsqlFromUrl(databaseUrl);

        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Не задана строка подключения: укажите DATABASE_URL или ConnectionStrings:DefaultConnection.");
    }

    /// <summary>
    /// Разбирает URL-строку подключения PaaS в <see cref="NpgsqlConnectionStringBuilder"/>.
    /// SSL включается принудительно (<c>Require</c>) — Render/Heroku требуют шифрованное
    /// соединение. В Npgsql 8 режим <c>Require</c> шифрует без строгой проверки сертификата,
    /// поэтому отдельный <c>Trust Server Certificate</c> не нужен.
    /// </summary>
    private static string BuildNpgsqlFromUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
