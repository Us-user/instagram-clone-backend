using Domain.Enums;

namespace Domain.DTOs.Notification;

/// <summary>
/// Сгруппированное уведомление для ленты. Одинаковые уведомления на один объект за
/// окно времени объединяются: <see cref="Actors"/> — первые (до 3) инициаторов,
/// <see cref="ActorsCount"/> — их общее число («X, Y и ещё N лайкнули ваше фото»).
/// </summary>
public class GetNotificationDto
{
    /// <summary>Id последнего (самого свежего) уведомления группы — для mark-as-read/delete.</summary>
    public int Id { get; set; }

    public NotificationType Type { get; set; }
    public NotificationEntityType EntityType { get; set; }

    /// <summary>Id объекта (пост/коммент/сторис). Для подписок — null.</summary>
    public int? EntityId { get; set; }

    /// <summary>Инициаторы (до 3, свежие первыми).</summary>
    public List<NotificationActorDto> Actors { get; set; } = new();

    /// <summary>Общее число уникальных инициаторов в группе.</summary>
    public int ActorsCount { get; set; }

    /// <summary>Группа считается прочитанной, только когда прочитаны все её уведомления.</summary>
    public bool IsRead { get; set; }

    /// <summary>Время самого свежего уведомления группы.</summary>
    public DateTime CreatedAt { get; set; }
}
