using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

/// <summary>
/// SignalR-хаб уведомлений. Уведомления создаются серверными сервисами (лайк/коммент/подписка/…),
/// а хаб только доставляет их получателю (сервер → клиент). Подключение идентифицируется по
/// JWT-claim userId (см. <see cref="CustomUserIdProvider"/>), адресация — через <c>Clients.User(...)</c>.
/// Клиенты подключаются по <c>/notificationHub</c> с токеном в query-параметре <c>access_token</c>.
/// </summary>
[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
}
