using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Streaming;

/// <summary>
/// Валидатор вебхуков LiveKit. LiveKit подписывает вебхук JWT в заголовке <c>Authorization</c>: токен
/// подписан <c>ApiSecret</c> (HMAC-SHA256), а его claim <c>sha256</c> содержит base64-хэш тела запроса.
/// Проверяем: (1) подпись токена нашим секретом; (2) совпадение SHA-256 тела с claim. Любое несовпадение
/// или отсутствие заголовка → <c>null</c> (запрос отклоняется). Неподписанные вебхуки не принимаются.
/// </summary>
public class LiveKitWebhookValidator : ILiveWebhookValidator
{
    private readonly LiveKitOptions _options;
    private readonly ILogger<LiveKitWebhookValidator> _logger;

    public LiveKitWebhookValidator(IOptions<StreamingOptions> options, ILogger<LiveKitWebhookValidator> logger)
    {
        _options = options.Value.LiveKit;
        _logger = logger;
    }

    public LiveWebhookEvent? Validate(string rawBody, string? authHeader)
    {
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            _logger.LogWarning("Вебхук LiveKit без заголовка Authorization — отклонён.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiSecret) || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("LiveKit не сконфигурирован — проверка подписи вебхука невозможна, отклонено.");
            return null;
        }

        // Заголовок может быть «Bearer <jwt>» или просто «<jwt>».
        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..].Trim()
            : authHeader.Trim();

        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            _logger.LogWarning("Вебхук LiveKit: некорректный формат токена — отклонён.");
            return null;
        }

        // (1) Проверка подписи токена нашим секретом.
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.ApiSecret));
        var expectedSig = hmac.ComputeHash(Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"));
        var providedSig = FromBase64Url(parts[2]);
        if (providedSig is null || !CryptographicOperations.FixedTimeEquals(expectedSig, providedSig))
        {
            _logger.LogWarning("Вебхук LiveKit: неверная подпись — отклонён.");
            return null;
        }

        // (2) Сверка SHA-256 тела с claim `sha256` из payload токена.
        try
        {
            var payloadJson = Encoding.UTF8.GetString(FromBase64Url(parts[1]) ?? Array.Empty<byte>());
            using var payload = JsonDocument.Parse(payloadJson);
            if (!payload.RootElement.TryGetProperty("sha256", out var shaClaim))
            {
                _logger.LogWarning("Вебхук LiveKit: в токене нет claim sha256 — отклонён.");
                return null;
            }

            var bodyHash = SHA256.HashData(Encoding.UTF8.GetBytes(rawBody));
            var expectedHash = Convert.ToBase64String(bodyHash);
            var claimHash = shaClaim.GetString() ?? string.Empty;

            // Сравниваем в обеих кодировках base64 (LiveKit использует стандартную; url-safe — на всякий случай).
            if (!FixedEquals(expectedHash, claimHash) && !FixedEquals(ToBase64Url(bodyHash), claimHash))
            {
                _logger.LogWarning("Вебхук LiveKit: хэш тела не совпал — отклонён (возможна подмена).");
                return null;
            }

            return Parse(rawBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Вебхук LiveKit: ошибка разбора токена/тела — отклонён.");
            return null;
        }
    }

    /// <summary>Разбирает тело вебхука LiveKit в модель события.</summary>
    private static LiveWebhookEvent? Parse(string rawBody)
    {
        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;

        var evt = root.TryGetProperty("event", out var e) ? e.GetString() : null;
        if (string.IsNullOrWhiteSpace(evt))
            return null;

        string? room = null;
        if (root.TryGetProperty("room", out var roomEl) && roomEl.ValueKind == JsonValueKind.Object
            && roomEl.TryGetProperty("name", out var roomName))
            room = roomName.GetString();

        string? identity = null;
        if (root.TryGetProperty("participant", out var pEl) && pEl.ValueKind == JsonValueKind.Object
            && pEl.TryGetProperty("identity", out var idEl))
            identity = idEl.GetString();

        var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        return new LiveWebhookEvent(evt!, room, identity, id);
    }

    private static bool FixedEquals(string a, string b) =>
        a.Length == b.Length && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));

    private static byte[]? FromBase64Url(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
            case 1: return null;
        }
        try { return Convert.FromBase64String(s); }
        catch { return null; }
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
