namespace Domain.Entities;

/// <summary>
/// Групповой чат (§7) — отдельная ветка от личных <see cref="Chat"/>. Создатель становится
/// стартовым админом. Группа остаётся группой даже с одним участником.
/// </summary>
public class GroupChat
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Имя файла аватара группы в wwwroot/images (nullable).</summary>
    public string? Avatar { get; set; }

    public string CreatorUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User? Creator { get; set; }
    public List<GroupChatMember> Members { get; set; } = new();
    public List<GroupMessage> Messages { get; set; } = new();
}
