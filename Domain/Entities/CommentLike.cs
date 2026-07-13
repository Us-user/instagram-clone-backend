namespace Domain.Entities;

/// <summary>Лайк комментария (Phase 14). Уникален на пару (Comment, User).</summary>
public class CommentLike
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public PostComment? Comment { get; set; }
    public User? User { get; set; }
}
