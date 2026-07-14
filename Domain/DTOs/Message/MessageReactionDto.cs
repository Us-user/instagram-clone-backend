namespace Domain.DTOs.Message;

/// <summary>Одна реакция на сообщение (§8): кто и каким эмодзи отреагировал.</summary>
public class MessageReactionDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
}
