namespace Domain.DTOs.Story;

/// <summary>Сводка по зрителям сторис (контракт — воспроизведено дословно).</summary>
public class ViewerDto
{
    public string UserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? ViewCount { get; set; }
    public int? ViewLike { get; set; }
}
