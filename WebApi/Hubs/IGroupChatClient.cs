using Domain.DTOs.GroupChat;

namespace WebApi.Hubs;

/// <summary>Строго типизированные методы, которые сервер вызывает у клиентов группового чата.</summary>
public interface IGroupChatClient
{
    /// <summary>Доставка нового (в т.ч. служебного) сообщения группы подключённому участнику.</summary>
    Task ReceiveGroupMessage(GetGroupMessageDto message);
}
