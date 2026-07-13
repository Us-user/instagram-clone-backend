namespace Domain.Entities;

/// <summary>Пост (обычный или reel).</summary>
public class Post
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsReel { get; set; }

    public User? User { get; set; }
    public List<PostImage> Images { get; set; } = new();
    public List<PostLike> Likes { get; set; } = new();
    public List<PostComment> Comments { get; set; } = new();
    public List<PostView> Views { get; set; } = new();
    public List<PostFavorite> Favorites { get; set; } = new();
}
