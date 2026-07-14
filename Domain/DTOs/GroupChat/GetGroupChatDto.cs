namespace Domain.DTOs.GroupChat;

/// <summary>Групповой чат в списке: инфо + последнее сообщение + непрочитанные для текущего юзера.</summary>
public class GetGroupChatDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public int MembersCount { get; set; }

    public string? LastMessage { get; set; }
    public DateTime? LastMessageDate { get; set; }
    public int UnreadCount { get; set; }
}
