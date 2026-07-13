using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Уведомление пользователю. Хранится по одной записи на действие; группировка
/// (несколько лайков на один пост → «X, Y и ещё N…») выполняется на выдаче,
/// в БД записи остаются раздельными (см. <c>NotificationService</c>).
/// </summary>
public class Notification
{
    public int Id { get; set; }

    /// <summary>Кому адресовано уведомление.</summary>
    public string RecipientUserId { get; set; } = string.Empty;

    /// <summary>Кто инициировал действие (лайкнул/подписался/прокомментировал).</summary>
    public string ActorUserId { get; set; } = string.Empty;

    public NotificationType Type { get; set; }
    public NotificationEntityType EntityType { get; set; }

    /// <summary>Id объекта (поста/коммента/сторис). Для <see cref="NotificationType.Follow"/> — null.</summary>
    public int? EntityId { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? Recipient { get; set; }
    public User? Actor { get; set; }
}
