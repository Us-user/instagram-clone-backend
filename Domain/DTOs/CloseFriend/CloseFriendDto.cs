namespace Domain.DTOs.CloseFriend;

/// <summary>Элемент списка «близких друзей» (§9): краткая карточка пользователя.</summary>
public class CloseFriendDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Avatar { get; set; }

    /// <summary>Верификация («синяя галочка») — инвариант: отдаём во всех DTO пользователя.</summary>
    public bool IsVerified { get; set; }

    public DateTime CreatedAt { get; set; }
}
