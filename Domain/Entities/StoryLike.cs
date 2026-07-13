namespace Domain.Entities;

/// <summary>Лайк сторис. Уникален на пару (Story, User).</summary>
public class StoryLike
{
    public int Id { get; set; }
    public int StoryId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public Story? Story { get; set; }
    public User? User { get; set; }
}
