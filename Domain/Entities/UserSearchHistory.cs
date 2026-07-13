namespace Domain.Entities;

/// <summary>История просмотренных профилей («смотрел профиль SearchedUser»).</summary>
public class UserSearchHistory
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string SearchedUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public User? SearchedUser { get; set; }
}
