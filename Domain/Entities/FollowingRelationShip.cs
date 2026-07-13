namespace Domain.Entities;

/// <summary>
/// Связь подписки: <see cref="UserId"/> (подписчик) подписан на <see cref="FollowingUserId"/> (на кого).
/// </summary>
public class FollowingRelationShip
{
    public int Id { get; set; }

    /// <summary>Подписчик.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>На кого подписан.</summary>
    public string FollowingUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public User? FollowingUser { get; set; }
}
