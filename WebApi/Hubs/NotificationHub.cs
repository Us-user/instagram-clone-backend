using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Hubs;

/// <summary>
/// SignalR-хаб уведомлений. Уведомления создаются серверными сервисами (лайк/коммент/подписка/…),
/// а хаб только доставляет их получателю (сервер → клиент). Подключение идентифицируется по
/// JWT-claim userId (см. <see cref="CustomUserIdProvider"/>), адресация — через <c>Clients.User(...)</c>.
/// Клиенты подключаются по <c>/notificationHub</c> с токеном в query-параметре <c>access_token</c>.
/// Наследует <see cref="PresenceAwareHub{TClient}"/> — обычно этот хаб открыт всё время работы
/// приложения, поэтому он же служит надёжным сигналом присутствия (§1).
/// </summary>
[Authorize]
public class NotificationHub : PresenceAwareHub<INotificationClient>
{
    public NotificationHub(IPresenceTracker tracker, IPresenceService presence)
        : base(tracker, presence)
    {
    }
}
