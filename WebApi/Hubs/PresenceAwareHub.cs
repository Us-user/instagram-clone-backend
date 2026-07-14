using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

/// <summary>
/// Базовый хаб, отслеживающий присутствие (§1). Любое подключение/отключение к любому хабу
/// (чат, группы, уведомления, presence) регистрируется в <see cref="IPresenceTracker"/>, так что
/// «онлайн» = приложение открыто и держит хотя бы одно соединение. На переходах офлайн↔онлайн
/// вызывает <see cref="IPresenceService"/> — тот обновляет <c>LastSeen</c> и рассылает статус.
/// Идентификация — по <see cref="HubCallerContext.UserIdentifier"/> (наш <c>CustomUserIdProvider</c>).
/// </summary>
public abstract class PresenceAwareHub<TClient> : Hub<TClient> where TClient : class
{
    private readonly IPresenceTracker _tracker;
    private readonly IPresenceService _presence;

    protected PresenceAwareHub(IPresenceTracker tracker, IPresenceService presence)
    {
        _tracker = tracker;
        _presence = presence;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId) && _tracker.Connect(userId, Context.ConnectionId))
            await _presence.OnUserOnlineAsync(userId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId) && _tracker.Disconnect(userId, Context.ConnectionId))
            await _presence.OnUserOfflineAsync(userId);

        await base.OnDisconnectedAsync(exception);
    }
}
