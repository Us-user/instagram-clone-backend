using Domain.DTOs.Notification;
using Domain.Enums;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Уведомления. Читающие методы работают для текущего юзера (Id из claims); лента
/// отдаётся сгруппированной по <c>(Type, EntityType, EntityId)</c> за окно времени.
/// <see cref="CreateAsync"/> вызывается другими сервисами при действиях (лайк, коммент,
/// подписка и т.п.): сохраняет запись, пушит real-time и пропускает действие «на себя».
/// </summary>
public interface INotificationService
{
    /// <summary>Сгруппированная лента уведомлений текущего юзера, с пагинацией.</summary>
    Task<PagedResponse<List<GetNotificationDto>>> GetNotificationsAsync(int? pageNumber, int? pageSize);

    /// <summary>Число непрочитанных уведомлений (по записям) текущего юзера.</summary>
    Task<Response<int>> GetUnreadCountAsync();

    /// <summary>Отметить одно уведомление прочитанным (только своё).</summary>
    Task<Response<bool>> MarkAsReadAsync(int? id);

    /// <summary>Отметить все уведомления текущего юзера прочитанными.</summary>
    Task<Response<bool>> MarkAllAsReadAsync();

    /// <summary>Удалить одно уведомление (только своё).</summary>
    Task<Response<bool>> DeleteNotificationAsync(int? id);

    /// <summary>
    /// Создать уведомление и запушить его получателю в реальном времени. Ничего не делает,
    /// если <paramref name="recipientUserId"/> совпадает с <paramref name="actorUserId"/>
    /// («не себе») или пуст. Вызывается из сервисов действий (Like/Comment/Follow/…).
    /// </summary>
    Task CreateAsync(
        string recipientUserId,
        string actorUserId,
        NotificationType type,
        NotificationEntityType entityType,
        int? entityId);
}
