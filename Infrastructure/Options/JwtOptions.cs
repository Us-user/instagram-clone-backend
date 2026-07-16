namespace Infrastructure.Options;

/// <summary>
/// Параметры JWT из секции "Jwt" appsettings.json.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Устаревший срок жизни единого JWT (до перехода на access+refresh). Оставлен для обратной
    /// совместимости конфигурации; фактически access-токен живёт <see cref="AccessTokenLifetimeMinutes"/>.
    /// </summary>
    public int LifetimeMinutes { get; set; } = 60;

    /// <summary>Срок жизни access-токена (JWT) в минутах. По ТЗ — 15.</summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    /// <summary>Срок жизни refresh-токена (и сессии) в днях. По ТЗ — 30.</summary>
    public int RefreshTokenLifetimeDays { get; set; } = 30;

    /// <summary>
    /// Максимум активных сессий на пользователя. При превышении при создании новой сессии
    /// отзывается самая старая по активности. 0 — без ограничения.
    /// </summary>
    public int MaxActiveSessionsPerUser { get; set; } = 10;
}
