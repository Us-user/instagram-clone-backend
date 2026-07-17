using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Enums;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Streaming;

/// <summary>
/// Основная реализация <see cref="IStreamingProvider"/> поверх LiveKit. Токены доступа генерируются как
/// подписанные (HMAC-SHA256) JWT с grants (<c>roomJoin</c>/<c>canPublish</c>/<c>canSubscribe</c>) —
/// это единственное, что реально нужно клиенту, чтобы подключиться к WebRTC-комнате. Управление комнатой
/// (create/update-role/remove/close) идёт через Server API (Twirp <c>RoomService</c>) и выполняется
/// <b>best-effort</b>: сбой не ломает бизнес-логику (видео вне бэкенда, LiveKit сам создаёт комнату при
/// первом входе и закрывает пустую). <c>ApiSecret</c> клиенту не отдаётся — только сгенерированный токен.
/// </summary>
public class LiveKitStreamingProvider : IStreamingProvider
{
    // Один HttpClient на процесс (рекомендованный паттерн — не плодим сокеты).
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly LiveKitOptions _options;
    private readonly ILogger<LiveKitStreamingProvider> _logger;

    public LiveKitStreamingProvider(IOptions<StreamingOptions> options, ILogger<LiveKitStreamingProvider> logger)
    {
        _options = options.Value.LiveKit;
        _logger = logger;
    }

    public async Task<string> CreateRoomAsync(string roomName)
    {
        await CallRoomServiceAsync("CreateRoom", new { name = roomName }, roomName);
        return roomName;
    }

    public Task<string> GenerateTokenAsync(string roomName, string userId, string userName, ParticipantRole role)
    {
        var canPublish = role == ParticipantRole.Publisher;
        var now = DateTimeOffset.UtcNow;

        var payload = new Dictionary<string, object?>
        {
            ["iss"] = _options.ApiKey,
            ["sub"] = userId,
            ["name"] = userName,
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(_options.TokenLifetimeMinutes).ToUnixTimeSeconds(),
            ["video"] = new Dictionary<string, object?>
            {
                ["room"] = roomName,
                ["roomJoin"] = true,
                ["canPublish"] = canPublish,
                ["canSubscribe"] = true,
                ["canPublishData"] = canPublish
            }
        };

        return Task.FromResult(SignJwt(payload));
    }

    public Task UpdateParticipantRoleAsync(string roomName, string userId, ParticipantRole role)
    {
        var canPublish = role == ParticipantRole.Publisher;
        return CallRoomServiceAsync("UpdateParticipant", new
        {
            room = roomName,
            identity = userId,
            permission = new
            {
                canPublish,
                canSubscribe = true,
                canPublishData = canPublish
            }
        }, roomName);
    }

    public Task RemoveParticipantAsync(string roomName, string userId) =>
        CallRoomServiceAsync("RemoveParticipant", new { room = roomName, identity = userId }, roomName);

    public Task CloseRoomAsync(string roomName) =>
        CallRoomServiceAsync("DeleteRoom", new { room = roomName }, roomName);

    /// <summary>
    /// Вызывает метод Twirp <c>RoomService</c> с админ-токеном. Best-effort: любая ошибка сети/HTTP
    /// логируется предупреждением и не пробрасывается — состояние эфира ведёт бэкенд, видео вне его.
    /// Если ключи не заданы (провайдер выбран, но не настроен) — тихо пропускаем серверный вызов.
    /// </summary>
    private async Task CallRoomServiceAsync(string method, object body, string roomName)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.ApiSecret)
            || string.IsNullOrWhiteSpace(_options.Url))
        {
            _logger.LogWarning("LiveKit не сконфигурирован (Url/ApiKey/ApiSecret) — метод {Method} пропущен.", method);
            return;
        }

        try
        {
            var url = $"{ToHttpUrl(_options.Url)}/twirp/livekit.RoomService/{method}";
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", BuildAdminToken(roomName));

            using var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("LiveKit {Method} → {Status}: {Body}", method, (int)response.StatusCode, text);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LiveKit {Method} для комнаты {Room} не выполнен (best-effort).", method, roomName);
        }
    }

    /// <summary>Админ-токен для Server API: grants roomCreate/roomAdmin на конкретную комнату.</summary>
    private string BuildAdminToken(string roomName)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new Dictionary<string, object?>
        {
            ["iss"] = _options.ApiKey,
            ["sub"] = _options.ApiKey,
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(10).ToUnixTimeSeconds(),
            ["video"] = new Dictionary<string, object?>
            {
                ["roomCreate"] = true,
                ["roomAdmin"] = true,
                ["roomList"] = true,
                ["room"] = roomName
            }
        };
        return SignJwt(payload);
    }

    /// <summary>Собирает подписанный HS256 JWT вручную, чтобы вложенный grant <c>video</c> был JSON-объектом.</summary>
    private string SignJwt(Dictionary<string, object?> payload)
    {
        var header = new Dictionary<string, object?> { ["alg"] = "HS256", ["typ"] = "JWT" };

        var headerSegment = Base64Url(JsonSerializer.SerializeToUtf8Bytes(header, JsonOptions));
        var payloadSegment = Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions));
        var signingInput = $"{headerSegment}.{payloadSegment}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.ApiSecret));
        var signature = hmac.ComputeHash(Encoding.ASCII.GetBytes(signingInput));

        return $"{signingInput}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Преобразует ws(s)-URL LiveKit в http(s) для Server API.</summary>
    private static string ToHttpUrl(string url)
    {
        var trimmed = url.TrimEnd('/');
        if (trimmed.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            return "https://" + trimmed["wss://".Length..];
        if (trimmed.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
            return "http://" + trimmed["ws://".Length..];
        return trimmed;
    }
}
