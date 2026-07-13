using Domain.DTOs.Notification;

namespace WebApi.Hubs;

/// <summary>Строго типизированные методы, которые сервер вызывает у клиентов уведомлений.</summary>
public interface INotificationClient
{
    /// <summary>Доставка нового уведомления получателю (живой «звоночек» и счётчик).</summary>
    Task ReceiveNotification(GetNotificationDto notification);
}
