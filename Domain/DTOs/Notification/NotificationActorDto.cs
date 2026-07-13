namespace Domain.DTOs.Notification;

/// <summary>
/// Инициатор уведомления для отрисовки аватара/имени в списке. Несколько актёров
/// в сгруппированном уведомлении отдаются первыми (самые свежие) + общий счётчик.
/// </summary>
public class NotificationActorDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public bool IsVerified { get; set; }
}
