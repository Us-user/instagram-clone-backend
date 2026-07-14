using Domain.DTOs.GroupChat;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

/// <summary>
/// Реализация <see cref="IGroupChatNotifier"/> поверх <see cref="GroupChatHub"/>. Живёт в WebApi
/// (там, где SignalR), а <c>GroupChatService</c> в Infrastructure зависит только от абстракции.
/// </summary>
public class GroupChatNotifier : IGroupChatNotifier
{
    private readonly IHubContext<GroupChatHub, IGroupChatClient> _hub;

    public GroupChatNotifier(IHubContext<GroupChatHub, IGroupChatClient> hub) => _hub = hub;

    public Task NotifyGroupMessageAsync(IReadOnlyCollection<string> memberUserIds, GetGroupMessageDto message) =>
        _hub.Clients.Users(memberUserIds.ToList()).ReceiveGroupMessage(message);
}
