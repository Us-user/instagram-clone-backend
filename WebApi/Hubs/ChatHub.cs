using Infrastructure.Constants;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Hubs;

/// <summary>
/// SignalR-хаб чата. Отправка сообщений идёт через REST (<c>PUT /Chat/send-message</c>),
/// а хаб доставляет их подключённым участникам (сервер → клиент) и принимает эфемерные события
/// набора (клиент → сервер, <see cref="Typing"/>). Подключение идентифицируется по JWT-claim userId
/// (см. <see cref="CustomUserIdProvider"/>), адресация — через <c>Clients.Users(...)</c>. Клиенты
/// подключаются по <c>/chatHub</c> с токеном в query-параметре <c>access_token</c>. Наследует
/// <see cref="PresenceAwareHub{TClient}"/> — соединение с чатом учитывается в присутствии (§1).
/// </summary>
[Authorize]
public class ChatHub : PresenceAwareHub<IChatClient>
{
    private readonly ITypingService _typing;

    public ChatHub(IPresenceTracker tracker, IPresenceService presence, ITypingService typing)
        : base(tracker, presence) => _typing = typing;

    /// <summary>
    /// Клиент сообщает, что печатает (или записывает голосовое) в личном чате <paramref name="chatId"/>.
    /// Событие эфемерное: сервер лишь ретранслирует его собеседнику. <paramref name="kind"/> ∈ {text, voice}.
    /// </summary>
    public Task Typing(int chatId, string? kind)
    {
        var userId = Context.UserIdentifier ?? string.Empty;
        var userName = Context.User?.FindFirst(CustomClaims.UserName)?.Value ?? string.Empty;
        return _typing.NotifyDirectTypingAsync(userId, userName, chatId, kind);
    }
}
