using Domain.Enums;

namespace Domain.DTOs.GroupChat;

/// <summary>Краткая цитата исходного сообщения при reply (§8).</summary>
public class GroupMessageReplyDto
{
    public int Id { get; set; }
    public string? SenderUserName { get; set; }
    public string? MessageText { get; set; }
    public MessageType MessageType { get; set; }
}
