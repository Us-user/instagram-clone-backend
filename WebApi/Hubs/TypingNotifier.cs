using Domain.DTOs.Presence;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

/// <summary>
/// Реализация <see cref="ITypingNotifier"/> поверх хабов чата и групп. Живёт в WebApi (там, где
/// SignalR), а <c>TypingService</c> в Infrastructure зависит только от абстракции. Личные события
/// уходят по <see cref="ChatHub"/> (событие <c>UserTyping</c>), групповые — по <see cref="GroupChatHub"/>
/// (событие <c>GroupTyping</c>).
/// </summary>
public class TypingNotifier : ITypingNotifier
{
    private readonly IHubContext<ChatHub, IChatClient> _chatHub;
    private readonly IHubContext<GroupChatHub, IGroupChatClient> _groupHub;

    public TypingNotifier(
        IHubContext<ChatHub, IChatClient> chatHub,
        IHubContext<GroupChatHub, IGroupChatClient> groupHub)
    {
        _chatHub = chatHub;
        _groupHub = groupHub;
    }

    public Task NotifyDirectTypingAsync(string recipientUserId, TypingDto typing) =>
        _chatHub.Clients.User(recipientUserId).UserTyping(typing);

    public Task NotifyGroupTypingAsync(IReadOnlyCollection<string> recipientUserIds, GroupTypingDto typing) =>
        _groupHub.Clients.Users(recipientUserIds.ToList()).GroupTyping(typing);
}
