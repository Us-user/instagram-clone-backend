using Domain.Enums;

namespace Infrastructure.Services.Streaming;

/// <summary>
/// Абстракция видео-провайдера прямых эфиров (WebRTC). Видео идёт напрямую между клиентом и провайдером,
/// минуя бэкенд; бэкенд лишь создаёт комнаты и раздаёт токены доступа с ролью по бизнес-логике.
/// Секретный ключ провайдера клиенту не отдаётся никогда. Реализации: <see cref="LiveKitStreamingProvider"/>
/// (основная) и <see cref="FakeStreamingProvider"/> (заглушка для локальной разработки/тестов).
/// </summary>
public interface IStreamingProvider
{
    /// <summary>Создаёт комнату у провайдера (best-effort — LiveKit создаёт комнату и при первом join).</summary>
    Task<string> CreateRoomAsync(string roomName);

    /// <summary>Генерирует токен доступа к комнате для участника с указанной ролью (grants по роли).</summary>
    Task<string> GenerateTokenAsync(string roomName, string userId, string userName, ParticipantRole role);

    /// <summary>Меняет роль участника в комнате (повышение до гостя / понижение до зрителя).</summary>
    Task UpdateParticipantRoleAsync(string roomName, string userId, ParticipantRole role);

    /// <summary>Удаляет участника из комнаты (кик при бане/убирании гостя).</summary>
    Task RemoveParticipantAsync(string roomName, string userId);

    /// <summary>Закрывает комнату (при завершении эфира) — всех отключает.</summary>
    Task CloseRoomAsync(string roomName);
}
