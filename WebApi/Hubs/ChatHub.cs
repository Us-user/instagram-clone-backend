using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

/// <summary>
/// SignalR-хаб чата. Отправка сообщений идёт через REST (<c>PUT /Chat/send-message</c>),
/// а хаб только доставляет их подключённым участникам (сервер → клиент). Подключение
/// идентифицируется по JWT-claim userId (см. <see cref="CustomUserIdProvider"/>), поэтому
/// адресация выполняется через <c>Clients.Users(...)</c>. Клиенты подключаются по <c>/chatHub</c>
/// с токеном в query-параметре <c>access_token</c>.
/// </summary>
[Authorize]
public class ChatHub : Hub<IChatClient>
{
}
