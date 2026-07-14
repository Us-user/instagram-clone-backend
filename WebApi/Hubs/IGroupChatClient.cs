using Domain.DTOs.GroupChat;
using Domain.DTOs.Message;
using Domain.DTOs.Presence;

namespace WebApi.Hubs;

/// <summary>Строго типизированные методы, которые сервер вызывает у клиентов группового чата.</summary>
public interface IGroupChatClient
{
    /// <summary>Доставка нового (в т.ч. служебного) сообщения группы подключённому участнику.</summary>
    Task ReceiveGroupMessage(GetGroupMessageDto message);

    /// <summary>Доставка обновлённого набора реакций сообщения группы (§8).</summary>
    Task ReceiveReaction(MessageReactionsDto reactions);

    /// <summary>Актуальный список печатающих в группе (§1): «X и ещё N печатают…».</summary>
    Task GroupTyping(GroupTypingDto typing);
}
