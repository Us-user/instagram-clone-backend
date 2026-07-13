namespace Domain.Entities;

/// <summary>Комментарий к посту.</summary>
public class PostComment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Post? Post { get; set; }
    public User? User { get; set; }
}
