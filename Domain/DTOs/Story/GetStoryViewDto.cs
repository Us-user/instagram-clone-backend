namespace Domain.DTOs.Story;

/// <summary>Результат фиксации просмотра сторис (контракт — дословно).</summary>
public class GetStoryViewDto
{
    public int Id { get; set; }
    public string ViewUserId { get; set; } = string.Empty;
    public int StoryId { get; set; }
}
