using Domain.Enums;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using UAParser;

namespace Infrastructure.Services;

/// <summary>
/// Определяет устройство/браузер/ОС и IP текущего запроса. User-Agent разбирается через UAParser;
/// тип устройства выводится эвристикой из семейства ОС/устройства. IP берётся из
/// <see cref="ConnectionInfo.RemoteIpAddress"/> с приоритетом первому адресу из <c>X-Forwarded-For</c>
/// (за обратным прокси/балансировщиком, как на Render/Heroku).
/// </summary>
public class DeviceInfoService : IDeviceInfoService
{
    // Parser.GetDefault() тяжёлый (компиляция regex-набора) — держим один экземпляр на процесс.
    private static readonly Parser UaParser = Parser.GetDefault();

    private readonly IHttpContextAccessor _accessor;

    public DeviceInfoService(IHttpContextAccessor accessor) => _accessor = accessor;

    public DeviceInfo GetCurrentDeviceInfo()
    {
        var context = _accessor.HttpContext;
        if (context is null)
            return new DeviceInfo(null, DeviceType.Unknown, null, null, string.Empty);

        var ip = ResolveIpAddress(context);
        var userAgent = context.Request.Headers.UserAgent.ToString();

        if (string.IsNullOrWhiteSpace(userAgent))
            return new DeviceInfo(null, DeviceType.Unknown, null, null, ip);

        var client = UaParser.Parse(userAgent);

        var browser = client.UA.Family is { Length: > 0 } and not "Other" ? client.UA.Family : null;
        var os = client.OS.Family is { Length: > 0 } and not "Other" ? client.OS.Family : null;

        // Метка «на чём»: конкретное устройство (iPhone/iPad/…), иначе ОС.
        var deviceLabel = client.Device.Family is { Length: > 0 } and not "Other"
            ? client.Device.Family
            : os;

        var deviceType = ResolveDeviceType(client, browser, os);

        string? deviceName = (browser, deviceLabel) switch
        {
            (not null, not null) => $"{browser} на {deviceLabel}",
            (not null, null) => browser,
            (null, not null) => deviceLabel,
            _ => null
        };

        return new DeviceInfo(deviceName, deviceType, browser, os, ip);
    }

    /// <summary>
    /// Эвристика типа устройства: мобильная ОС или явно мобильное устройство → Mobile; десктопная ОС
    /// с распознанным браузером → Web; десктопная ОС без браузера (нативный клиент/утилита) → Desktop;
    /// иначе Unknown. Важно НЕ считать мобильным любое непустое семейство устройства: у десктопных Mac
    /// UAParser отдаёт Device.Family = «Mac», поэтому опираемся только на явные мобильные признаки.
    /// </summary>
    private static DeviceType ResolveDeviceType(ClientInfo client, string? browser, string? os)
    {
        var osFamily = client.OS.Family ?? string.Empty;
        var deviceFamily = client.Device.Family ?? string.Empty;

        var isMobileOs = osFamily.Contains("iOS", StringComparison.OrdinalIgnoreCase)
                         || osFamily.Contains("Android", StringComparison.OrdinalIgnoreCase)
                         || osFamily.Contains("Windows Phone", StringComparison.OrdinalIgnoreCase)
                         || osFamily.Contains("BlackBerry", StringComparison.OrdinalIgnoreCase)
                         || osFamily.Contains("KaiOS", StringComparison.OrdinalIgnoreCase);

        var isMobileDevice = deviceFamily.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
                             || deviceFamily.Contains("iPad", StringComparison.OrdinalIgnoreCase)
                             || deviceFamily.Contains("iPod", StringComparison.OrdinalIgnoreCase)
                             || deviceFamily.Contains("Mobile", StringComparison.OrdinalIgnoreCase)
                             || deviceFamily.Contains("Tablet", StringComparison.OrdinalIgnoreCase);

        if (isMobileOs || isMobileDevice)
            return DeviceType.Mobile;

        if (os is not null)
            return browser is not null ? DeviceType.Web : DeviceType.Desktop;

        return DeviceType.Unknown;
    }

    private static string ResolveIpAddress(HttpContext context)
    {
        // За прокси реальный клиент — первый адрес в X-Forwarded-For (client, proxy1, proxy2, …).
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
                return first;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }
}
