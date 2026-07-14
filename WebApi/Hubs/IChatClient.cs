using Domain.DTOs.Chat;
using Domain.DTOs.Message;

namespace WebApi.Hubs;

/// <summary>Строго типизированные методы, которые сервер вызывает у клиентов чата.</summary>
public interface IChatClient
{
    /// <summary>Доставка нового сообщения подключённому участнику чата.</summary>
    Task ReceiveMessage(GetMessageDto message);

    /// <summary>Доставка обновлённого набора реакций сообщения (§8).</summary>
    Task ReceiveReaction(MessageReactionsDto reactions);
}
