using Domain.DTOs.GroupChat;
using Domain.DTOs.Message;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Абстракция реал-тайм доставки сообщений группового чата. Реализация живёт в WebApi
/// (SignalR-хаб), чтобы слой Infrastructure не зависел от WebApi. <see cref="IGroupChatService"/>
/// вызывает её после сохранения сообщения (в т.ч. служебного).
/// </summary>
public interface IGroupChatNotifier
{
    /// <summary>Доставить новое сообщение группы всем перечисленным участникам в реальном времени.</summary>
    Task NotifyGroupMessageAsync(IReadOnlyCollection<string> memberUserIds, GetGroupMessageDto message);

    /// <summary>Доставить обновлённый набор реакций сообщения всем участникам группы (§8).</summary>
    Task NotifyReactionAsync(IReadOnlyCollection<string> memberUserIds, MessageReactionsDto reactions);
}
