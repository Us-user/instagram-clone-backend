namespace Domain.DTOs.Hashtag;

/// <summary>Хэштег для чтения: тег, счётчик постов, дата создания.</summary>
public class GetHashtagDto
{
    public int Id { get; set; }
    public string Tag { get; set; } = string.Empty;
    public int PostsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
