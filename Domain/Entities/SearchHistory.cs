namespace Domain.Entities;

/// <summary>История текстового поиска пользователя.</summary>
public class SearchHistory
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
