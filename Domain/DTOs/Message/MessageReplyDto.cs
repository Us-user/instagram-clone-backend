using Domain.Enums;

namespace Domain.DTOs.Message;

/// <summary>Краткая цитата исходного сообщения личного чата при reply (§8).</summary>
public class MessageReplyDto
{
    public int Id { get; set; }
    public string? SenderUserId { get; set; }
    public string? SenderUserName { get; set; }
    public string? MessageText { get; set; }
    public MessageType MessageType { get; set; }
}
