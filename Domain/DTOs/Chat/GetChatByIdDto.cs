namespace Domain.DTOs.Chat;

/// <summary>Чат с полной перепиской.</summary>
public class GetChatByIdDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserImage { get; set; }
    public List<GetMessageDto> Messages { get; set; } = new();
}
