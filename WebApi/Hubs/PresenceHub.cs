using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Hubs;

/// <summary>
/// SignalR-хаб присутствия (§1). Клиент держит соединение (что само по себе делает его «онлайн»
/// через <see cref="PresenceAwareHub{TClient}"/>) и получает real-time обновления статусов тех,
/// с кем связан (собеседники по личным чатам и участники общих групп). Сами статусы можно и
/// запросить через REST (<c>/Presence/get-status</c>, <c>/Presence/get-statuses</c>). Подключение —
/// по <c>/presenceHub</c> с токеном в query-параметре <c>access_token</c>.
/// </summary>
[Authorize]
public class PresenceHub : PresenceAwareHub<IPresenceClient>
{
    public PresenceHub(IPresenceTracker tracker, IPresenceService presence)
        : base(tracker, presence)
    {
    }
}
