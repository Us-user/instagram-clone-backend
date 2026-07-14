using Infrastructure.Constants;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Hubs;

/// <summary>
/// SignalR-хаб групповых чатов (§7). Отправка идёт через REST (<c>PUT /GroupChat/send-message</c>),
/// а хаб доставляет сообщения подключённым участникам (сервер → клиент) и принимает эфемерные
/// события набора (клиент → сервер, <see cref="Typing"/>). Подключение идентифицируется по JWT-claim
/// userId (см. <see cref="CustomUserIdProvider"/>), адресация — через <c>Clients.Users(...)</c>.
/// Клиенты подключаются по <c>/groupChatHub</c> с токеном в query-параметре <c>access_token</c>.
/// Наследует <see cref="PresenceAwareHub{TClient}"/> — соединение учитывается в присутствии (§1).
/// </summary>
[Authorize]
public class GroupChatHub : PresenceAwareHub<IGroupChatClient>
{
    private readonly ITypingService _typing;

    public GroupChatHub(IPresenceTracker tracker, IPresenceService presence, ITypingService typing)
        : base(tracker, presence) => _typing = typing;

    /// <summary>
    /// Клиент сообщает, что печатает (или записывает голосовое) в группе <paramref name="groupChatId"/>.
    /// Сервер обновляет эфемерный список печатающих и рассылает его остальным участникам
    /// («X и ещё N печатают…»). <paramref name="kind"/> ∈ {text, voice}.
    /// </summary>
    public Task Typing(int groupChatId, string? kind)
    {
        var userId = Context.UserIdentifier ?? string.Empty;
        var userName = Context.User?.FindFirst(CustomClaims.UserName)?.Value ?? string.Empty;
        return _typing.NotifyGroupTypingAsync(userId, userName, groupChatId, kind);
    }
}
