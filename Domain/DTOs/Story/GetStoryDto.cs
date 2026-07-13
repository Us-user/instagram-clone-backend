namespace Domain.DTOs.Story;

/// <summary>
/// Сторис для чтения (контракт — воспроизведено дословно, включая имя поля <c>createAt</c>).
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
}
