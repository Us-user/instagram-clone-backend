namespace Domain.DTOs.FollowingRelationShip;

/// <summary>Входящий запрос на подписку (для владельца приватного аккаунта).</summary>
public class GetFollowRequestDto
{
    /// <summary>Id пользователя, приславшего запрос.</summary>
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public bool IsVerified { get; set; }

    /// <summary>Когда был отправлен запрос.</summary>
    public DateTime CreatedAt { get; set; }
}
