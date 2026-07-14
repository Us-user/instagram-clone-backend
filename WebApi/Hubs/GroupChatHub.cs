using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

/// <summary>
/// SignalR-хаб групповых чатов (§7). Отправка идёт через REST
/// (<c>PUT /GroupChat/send-message</c>), а хаб только доставляет сообщения подключённым
/// участникам (сервер → клиент). Подключение идентифицируется по JWT-claim userId
/// (см. <see cref="CustomUserIdProvider"/>), адресация — через <c>Clients.Users(...)</c>.
/// Клиенты подключаются по <c>/groupChatHub</c> с токеном в query-параметре <c>access_token</c>.
/// </summary>
[Authorize]
public class GroupChatHub : Hub<IGroupChatClient>
{
}
