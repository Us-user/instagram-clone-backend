using Domain.DTOs.Chat;

namespace WebApi.Hubs;

/// <summary>Строго типизированные методы, которые сервер вызывает у клиентов чата.</summary>
public interface IChatClient
{
    /// <summary>Доставка нового сообщения подключённому участнику чата.</summary>
    Task ReceiveMessage(GetMessageDto message);
}
