using Domain.Enums;

namespace Infrastructure.Services.Interfaces;

/// <summary>Разобранные данные устройства текущего запроса (User-Agent + IP).</summary>
/// <param name="DeviceName">Человекочитаемое имя, напр. «Chrome на Windows».</param>
/// <param name="DeviceType">Тип устройства (Mobile/Desktop/Web/Unknown).</param>
/// <param name="Browser">Семейство браузера (или null).</param>
/// <param name="Os">Операционная система (или null).</param>
/// <param name="IpAddress">IP-адрес (с учётом X-Forwarded-For).</param>
public readonly record struct DeviceInfo(
    string? DeviceName,
    DeviceType DeviceType,
    string? Browser,
    string? Os,
    string IpAddress);

/// <summary>
/// Определяет устройство/IP текущего HTTP-запроса по заголовку User-Agent и адресу соединения.
/// Реализация парсит User-Agent (UAParser) и учитывает X-Forwarded-For за прокси.
/// </summary>
public interface IDeviceInfoService
{
    /// <summary>Разбирает текущий запрос в <see cref="DeviceInfo"/>. При отсутствии контекста — Unknown.</summary>
    DeviceInfo GetCurrentDeviceInfo();
}
