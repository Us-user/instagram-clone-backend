using Domain.DTOs.GroupChat;
using Domain.DTOs.Message;

namespace WebApi.Hubs;

/// <summary>Строго типизированные методы, которые сервер вызывает у клиентов группового чата.</summary>
public interface IGroupChatClient
{
    /// <summary>Доставка нового (в т.ч. служебного) сообщения группы подключённому участнику.</summary>
    Task ReceiveGroupMessage(GetGroupMessageDto message);

    /// <summary>Доставка обновлённого набора реакций сообщения группы (§8).</summary>
    Task ReceiveReaction(MessageReactionsDto reactions);
}
