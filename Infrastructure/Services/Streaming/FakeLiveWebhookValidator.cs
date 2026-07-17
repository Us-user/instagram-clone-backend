using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Streaming;

/// <summary>
/// Валидатор вебхуков для <see cref="FakeStreamingProvider"/> (локальная разработка/тесты). Подписи нет —
/// тело разбирается как есть, чтобы можно было вручную дёргать <c>POST /Live/webhook</c> и проверять
/// синхронизацию состояния без реального LiveKit. В проде активен <see cref="LiveKitWebhookValidator"/>
/// с обязательной проверкой подписи.
/// </summary>
public class FakeLiveWebhookValidator : ILiveWebhookValidator
{
    private readonly ILogger<FakeLiveWebhookValidator> _logger;

    public FakeLiveWebhookValidator(ILogger<FakeLiveWebhookValidator> logger) => _logger = logger;

    public LiveWebhookEvent? Validate(string rawBody, string? authHeader)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return null;

        try
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
            else if (root.TryGetProperty("room", out var roomStr) && roomStr.ValueKind == JsonValueKind.String)
                room = roomStr.GetString();

            string? identity = null;
            if (root.TryGetProperty("participant", out var pEl) && pEl.ValueKind == JsonValueKind.Object
                && pEl.TryGetProperty("identity", out var idEl))
                identity = idEl.GetString();
            else if (root.TryGetProperty("identity", out var idStr) && idStr.ValueKind == JsonValueKind.String)
                identity = idStr.GetString();

            var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

            _logger.LogInformation("FakeStreaming: принят вебхук {Event} (без проверки подписи, dev-режим).", evt);
            return new LiveWebhookEvent(evt!, room, identity, id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FakeStreaming: не удалось разобрать тело вебхука.");
            return null;
        }
    }
}
