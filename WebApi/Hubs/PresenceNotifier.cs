using Domain.DTOs.Presence;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

/// <summary>
/// Реализация <see cref="IPresenceNotifier"/> поверх <see cref="PresenceHub"/>. Живёт в WebApi
/// (там, где SignalR), а <c>PresenceService</c> в Infrastructure зависит только от абстракции.
/// </summary>
public class PresenceNotifier : IPresenceNotifier
{
    private readonly IHubContext<PresenceHub, IPresenceClient> _hub;

    public PresenceNotifier(IHubContext<PresenceHub, IPresenceClient> hub) => _hub = hub;

    public Task NotifyPresenceAsync(IReadOnlyCollection<string> recipientUserIds, UserPresenceDto presence) =>
        _hub.Clients.Users(recipientUserIds.ToList()).ReceivePresence(presence);
}
