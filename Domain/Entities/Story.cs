using Domain.Enums;

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

    /// <summary>Аудитория сторис (§9): все или только «близкие друзья» автора.</summary>
    public StoryAudience Audience { get; set; }

    /// <summary>
    /// Репост чужого публичного поста в сторис (§9, nullable): сторис показывает превью поста
    /// и ведёт к оригиналу. Отличается от <see cref="PostId"/> (сторис, собранная из поста).
    /// </summary>
    public int? SharedPostId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public Post? Post { get; set; }

    /// <summary>Оригинал репостнутого поста (для <see cref="SharedPostId"/>).</summary>
    public Post? SharedPost { get; set; }

    public List<StoryLike> Likes { get; set; } = new();
    public List<StoryView> Views { get; set; } = new();
}
