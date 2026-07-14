using Domain.Enums;

namespace Domain.DTOs.Story;

/// <summary>
/// Сторис для чтения (контракт — воспроизведено дословно, включая имя поля <c>createAt</c>).
/// Поля §9 (<see cref="Audience"/>/<see cref="SharedPostId"/>/<see cref="SharedPost"/>) —
/// необязательные расширения, контракт базы не ломают.
/// </summary>
public class GetStoryDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int? PostId { get; set; }
    public DateTime CreateAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserAvatar { get; set; } = string.Empty;
    public ViewerDto ViewerDto { get; set; } = new();

    /// <summary>Аудитория сторис (§9).</summary>
    public StoryAudience Audience { get; set; }

    /// <summary>Id репостнутого в сторис поста (§9, nullable).</summary>
    public int? SharedPostId { get; set; }

    /// <summary>Превью репостнутого поста (§9, nullable — заполнено только для репоста).</summary>
    public SharedPostPreviewDto? SharedPost { get; set; }
}
