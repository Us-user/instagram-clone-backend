namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Примерная геолокация по IP (город/страна). Абстракция намеренно тривиальна: реализация может
/// быть заглушкой или на базе локальной GeoIP-базы. При недоступности возвращает <c>null</c> —
/// флоу логина ломаться не должен.
/// </summary>
public interface IGeoLocationService
{
    /// <summary>Примерная локация («Город, Страна») по IP или <c>null</c>, если определить нельзя.</summary>
    Task<string?> GetLocationAsync(string? ipAddress, CancellationToken cancellationToken = default);
}
