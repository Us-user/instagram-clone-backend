using Domain.DTOs.Notification;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Абстракция real-time доставки уведомлений. Реализация живёт в WebApi (SignalR-хаб
/// <c>NotificationHub</c>), чтобы слой Infrastructure не зависел от WebApi.
/// <see cref="INotificationService"/> вызывает её после сохранения уведомления.
/// </summary>
public interface INotificationNotifier
{
    /// <summary>Доставить новое уведомление получателю (живой «звоночек» и счётчик).</summary>
    Task NotifyAsync(string recipientUserId, GetNotificationDto notification);
}
