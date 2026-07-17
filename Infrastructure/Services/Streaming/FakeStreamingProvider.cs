using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Streaming;

/// <summary>
/// Заглушка видео-провайдера для локальной разработки и тестов без реального WebRTC. Возвращает
/// фиктивные, но структурно правдоподобные токены (<c>fake.{role}.{room}.{userId}</c>), чтобы бизнес-логику
/// эфиров (доступ, зрители, комменты, гости, статистика) можно было проверять изолированно. Операции
/// управления комнатой — no-op с логированием.
/// </summary>
public class FakeStreamingProvider : IStreamingProvider
{
    private readonly ILogger<FakeStreamingProvider> _logger;

    public FakeStreamingProvider(ILogger<FakeStreamingProvider> logger) => _logger = logger;

    public Task<string> CreateRoomAsync(string roomName)
    {
        _logger.LogInformation("FakeStreaming: комната {Room} создана (заглушка).", roomName);
        return Task.FromResult(roomName);
    }

    public Task<string> GenerateTokenAsync(string roomName, string userId, string userName, ParticipantRole role)
    {
        // Фиктивный токен: не JWT, но детерминированный и различимый по роли — хватает для проверки логики.
        var token = $"fake.{role.ToString().ToLowerInvariant()}.{roomName}.{userId}";
        return Task.FromResult(token);
    }

    public Task UpdateParticipantRoleAsync(string roomName, string userId, ParticipantRole role)
    {
        _logger.LogInformation("FakeStreaming: участник {User} в {Room} → роль {Role} (заглушка).",
            userId, roomName, role);
        return Task.CompletedTask;
    }

    public Task RemoveParticipantAsync(string roomName, string userId)
    {
        _logger.LogInformation("FakeStreaming: участник {User} удалён из {Room} (заглушка).", userId, roomName);
        return Task.CompletedTask;
    }

    public Task CloseRoomAsync(string roomName)
    {
        _logger.LogInformation("FakeStreaming: комната {Room} закрыта (заглушка).", roomName);
        return Task.CompletedTask;
    }
}
