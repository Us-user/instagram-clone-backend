using Domain.Enums;

namespace Domain.DTOs.GroupChat;

/// <summary>Участник группы для выдачи: пользователь + роль.</summary>
public class GroupMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserImage { get; set; }
    public bool IsVerified { get; set; }
    public GroupMemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
