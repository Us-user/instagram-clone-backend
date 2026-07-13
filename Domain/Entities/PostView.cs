namespace Domain.Entities;

/// <summary>Просмотр поста. Уникален на пару (Post, User).</summary>
public class PostView
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public Post? Post { get; set; }
    public User? User { get; set; }
}
