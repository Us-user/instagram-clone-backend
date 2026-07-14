using Domain.Enums;

namespace Domain.DTOs.Message;

/// <summary>
/// Полный набор реакций на конкретное сообщение (§8). Возвращается эндпоинтом <c>react</c> и
/// рассылается в реальном времени участникам, чтобы клиент обновил состояние реакций сообщения.
/// </summary>
public class MessageReactionsDto
{
    public int MessageId { get; set; }
    public MessageContext Context { get; set; }
    public List<MessageReactionDto> Reactions { get; set; } = new();
}
