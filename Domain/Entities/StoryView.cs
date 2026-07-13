namespace Domain.Entities;

/// <summary>Просмотр сторис. Уникален на пару (Story, ViewUser).</summary>
public class StoryView
{
    public int Id { get; set; }
    public int StoryId { get; set; }
    public string ViewUserId { get; set; } = string.Empty;

    public Story? Story { get; set; }
    public User? ViewUser { get; set; }
}
