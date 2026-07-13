namespace Domain.DTOs.Chat;

/// <summary>Сообщение чата для чтения.</summary>
public class GetMessageDto
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public string SenderUserId { get; set; } = string.Empty;
    public string? MessageText { get; set; }
    public string? FileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
