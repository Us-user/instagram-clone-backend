using Domain.DTOs.Chat;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

/// <summary>
/// Реализация <see cref="IChatNotifier"/> поверх <see cref="ChatHub"/>. Живёт в WebApi (там,
/// где SignalR), а сервис чата в Infrastructure зависит только от абстракции — слои не связаны.
/// </summary>
public class ChatNotifier : IChatNotifier
{
    private readonly IHubContext<ChatHub, IChatClient> _hub;

    public ChatNotifier(IHubContext<ChatHub, IChatClient> hub) => _hub = hub;

    public Task NotifyMessageAsync(string user1Id, string user2Id, GetMessageDto message) =>
        _hub.Clients.Users(new[] { user1Id, user2Id }).ReceiveMessage(message);
}
