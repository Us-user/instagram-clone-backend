namespace Domain.DTOs.Session;

/// <summary>
/// Активная сессия текущего пользователя (для <c>GET /Session/get-active-sessions</c>).
/// <see cref="IsCurrent"/> — сессия, из которой сделан запрос (по claim <c>sessionId</c>).
/// </summary>
public class SessionDto
{
    public Guid Id { get; set; }
    public string? DeviceName { get; set; }

    /// <summary>Тип устройства строкой (<c>Mobile</c>/<c>Desktop</c>/<c>Web</c>/<c>Unknown</c>).</summary>
    public string DeviceType { get; set; } = string.Empty;

    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }

    /// <summary>Текущая ли это сессия (та, из которой выполнен запрос).</summary>
    public bool IsCurrent { get; set; }
}
