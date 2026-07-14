using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Участник группового чата (§7) с ролью. Пара (GroupChatId, UserId) уникальна.
/// <see cref="LastReadAt"/> хранит момент последнего открытия группы участником —
/// по нему считаются непрочитанные сообщения.
/// </summary>
public class GroupChatMember
{
    public int Id { get; set; }
    public int GroupChatId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public GroupMemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    /// <summary>Когда участник последний раз открывал группу (для подсчёта непрочитанных). null — ещё не открывал.</summary>
    public DateTime? LastReadAt { get; set; }

    public GroupChat? GroupChat { get; set; }
    public User? User { get; set; }
}
