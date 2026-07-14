using Domain.Enums;

namespace Domain.DTOs.GroupChat;

/// <summary>Полная карточка группы: инфо + участники + сообщения + роль текущего юзера.</summary>
public class GetGroupChatByIdDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string CreatorUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>Роль текущего пользователя в этой группе.</summary>
    public GroupMemberRole MyRole { get; set; }

    public List<GroupMemberDto> Members { get; set; } = new();
    public List<GetGroupMessageDto> Messages { get; set; } = new();
}
