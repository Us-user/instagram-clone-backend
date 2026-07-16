using Infrastructure.Services.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Заглушка геолокации: всегда возвращает <c>null</c> (кроме очевидно локальных адресов). Внешние
/// GeoIP-сервисы в учебном проекте не подключаются; при необходимости эту реализацию можно заменить
/// на основанную на локальной GeoIP-базе, не трогая вызывающий код (модуль сессий зависит от
/// <see cref="IGeoLocationService"/>). Отсутствие локации не ломает создание сессии.
/// </summary>
public class GeoLocationService : IGeoLocationService
{
    public Task<string?> GetLocationAsync(string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Task.FromResult<string?>(null);

        if (ipAddress is "::1" or "127.0.0.1" || ipAddress.StartsWith("::ffff:127."))
            return Task.FromResult<string?>("Локальная сеть");

        return Task.FromResult<string?>(null);
    }
}
