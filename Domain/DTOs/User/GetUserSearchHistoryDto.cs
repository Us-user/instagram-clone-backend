namespace Domain.DTOs.User;

/// <summary>Запись истории просмотренных профилей.</summary>
public class GetUserSearchHistoryDto
{
    public int Id { get; set; }
    public string SearchedUserId { get; set; } = string.Empty;
    public string SearchedUserName { get; set; } = string.Empty;
    public string? SearchedUserAvatar { get; set; }
    public DateTime CreatedAt { get; set; }
}
