using Domain.DTOs.Chat;
using Domain.DTOs.Message;
using Domain.DTOs.Presence;

namespace WebApi.Hubs;

/// <summary>Строго типизированные методы, которые сервер вызывает у клиентов чата.</summary>
public interface IChatClient
{
    /// <summary>Доставка нового сообщения подключённому участнику чата.</summary>
    Task ReceiveMessage(GetMessageDto message);

    /// <summary>Доставка обновлённого набора реакций сообщения (§8).</summary>
    Task ReceiveReaction(MessageReactionsDto reactions);

    /// <summary>Событие «печатает…»/«записывает голосовое…» от собеседника в личном чате (§1).</summary>
    Task UserTyping(TypingDto typing);
}
