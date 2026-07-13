namespace Domain.Entities;

/// <summary>Лайк поста. Уникален на пару (Post, User).</summary>
public class PostLike
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Post? Post { get; set; }
    public User? User { get; set; }
}
