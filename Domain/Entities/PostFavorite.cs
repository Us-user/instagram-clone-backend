namespace Domain.Entities;

/// <summary>Пост в избранном пользователя. Уникален на пару (Post, User).</summary>
public class PostFavorite
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Post? Post { get; set; }
    public User? User { get; set; }
}
