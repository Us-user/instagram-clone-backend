using Domain.DTOs.Notification;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

/// <summary>
/// Реализация <see cref="INotificationNotifier"/> поверх <see cref="NotificationHub"/>. Живёт в
/// WebApi (там, где SignalR), а <c>NotificationService</c> в Infrastructure зависит только от
/// абстракции — слои не связаны.
/// </summary>
public class NotificationNotifier : INotificationNotifier
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hub;

    public NotificationNotifier(IHubContext<NotificationHub, INotificationClient> hub) => _hub = hub;

    public Task NotifyAsync(string recipientUserId, GetNotificationDto notification) =>
        _hub.Clients.User(recipientUserId).ReceiveNotification(notification);
}
