namespace Domain.DTOs.Story;

/// <summary>Сторис одного автора, сгруппированные для ленты сторис.</summary>
public class GetStoriesDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public List<StoryItemDto> Stories { get; set; } = new();
}

/// <summary>Отдельная сторис внутри группы автора.</summary>
public class StoryItemDto
{
    public int Id { get; set; }
    public string? FileName { get; set; }
    public int? PostId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LikeCount { get; set; }
    public int ViewCount { get; set; }
}
