using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi.Hubs;

/// <summary>
/// SignalR-хаб прямого эфира (§6). Клиент вызывает <see cref="JoinStream"/>/<see cref="LeaveStream"/>,
/// чтобы войти/выйти из группы <c>live_{streamId}</c> и получать real-time события (комменты, зрители,
/// сердечки, заявки). Учёт зрителей в БД ведёт REST (<c>join</c>/<c>leave</c>); хаб отвечает за доставку
/// событий и за обработку обрыва связи: при потере соединения зритель считается вышедшим с грейс-периодом
/// <see cref="GracePeriod"/> на переподключение (моргание сети не ломает статистику). Наследует
/// <see cref="PresenceAwareHub{TClient}"/> — соединение учитывается в присутствии (§1). Токен — из query
/// <c>access_token</c> (см. настройку JWT для хабов).
/// </summary>
[Authorize]
public class LiveHub : PresenceAwareHub<ILiveClient>
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(30);

    private readonly ILiveConnectionTracker _connections;
    private readonly IServiceScopeFactory _scopeFactory;

    public LiveHub(
        IPresenceTracker tracker,
        IPresenceService presence,
        ILiveConnectionTracker connections,
        IServiceScopeFactory scopeFactory)
        : base(tracker, presence)
    {
        _connections = connections;
        _scopeFactory = scopeFactory;
    }

    /// <summary>Имя SignalR-группы эфира.</summary>
    public static string GroupName(int streamId) => $"live_{streamId}";

    /// <summary>Клиент входит в группу эфира (после успешного REST <c>join</c>).</summary>
    public async Task JoinStream(int streamId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(streamId));
        _connections.Add(Context.ConnectionId, streamId, userId);
    }

    /// <summary>Клиент явно покидает группу эфира.</summary>
    public async Task LeaveStream(int streamId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(streamId));
        _connections.Remove(Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Снимаем привязку и, если у зрителя не осталось живых соединений к эфиру после грейс-периода,
        // фиксируем выход. Присутствие (§1) обрабатывает базовый класс.
        var mapping = _connections.Remove(Context.ConnectionId);
        if (mapping is { } m)
            ScheduleDisconnectGrace(m.StreamId, m.UserId);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Через грейс-период проверяет, не переподключился ли зритель; если нет — помечает выход в БД и
    /// рассылает пересчитанный счётчик. Fire-and-forget с собственным DI-scope (хаб уже уничтожен).
    /// </summary>
    private void ScheduleDisconnectGrace(int streamId, string userId)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(GracePeriod);
            if (_connections.IsUserWatching(streamId, userId))
                return; // переподключился — выход не фиксируем

            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ILiveStreamService>();
            await service.HandleViewerDisconnectAsync(streamId, userId);
        });
    }
}
