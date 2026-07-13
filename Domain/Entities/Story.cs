namespace Domain.Entities;

/// <summary>Сторис. Живёт 24 часа от <see cref="CreatedAt"/>. Может быть из файла или из поста.</summary>
public class Story
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>Имя файла сторис в wwwroot/images (nullable, если сторис из поста).</summary>
    public string? FileName { get; set; }

    /// <summary>Пост-источник сторис (nullable).</summary>
    public int? PostId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public Post? Post { get; set; }
    public List<StoryLike> Likes { get; set; } = new();
    public List<StoryView> Views { get; set; } = new();
}
