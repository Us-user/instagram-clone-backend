namespace Domain.DTOs.Chat;

/// <summary>Чат в списке: собеседник + последнее сообщение + непрочитанные.</summary>
public class GetChatDto
{
    public int Id { get; set; }

    /// <summary>Собеседник текущего пользователя.</summary>
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserImage { get; set; }

    public string? LastMessage { get; set; }
    public DateTime? LastMessageDate { get; set; }
    public int UnreadCount { get; set; }
}
