namespace Domain.DTOs.Live;

/// <summary>
/// Комментарий эфира — для истории (<c>get-comments</c>) и для real-time события <c>NewComment</c>.
/// Удалённые (soft-delete) в истории не отдаются.
/// </summary>
public class LiveCommentDto
{
    public int Id { get; set; }
    public LiveUserDto User { get; set; } = new();
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPinned { get; set; }
}
