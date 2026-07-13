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

    /// <summary>Срок жизни токена в минутах.</summary>
    public int LifetimeMinutes { get; set; } = 60;
}
