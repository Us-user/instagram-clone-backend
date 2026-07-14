namespace Domain.DTOs.Story;

/// <summary>
/// Превью репостнутого в сторис поста (§9): даёт клиенту минимум для отрисовки карточки
/// (id поста, автор, первое изображение) и перехода к оригиналу.
/// </summary>
public class SharedPostPreviewDto
{
    public int PostId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ImageName { get; set; }
}
