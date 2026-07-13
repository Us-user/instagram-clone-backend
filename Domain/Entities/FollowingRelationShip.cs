using Domain.Enums;

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

    /// <summary>
    /// Статус подписки (Phase 12). На публичный аккаунт — сразу <see cref="FollowStatus.Accepted"/>,
    /// на приватный — <see cref="FollowStatus.Pending"/> до одобрения владельцем. Существующие
    /// связи базы после миграции считаются <see cref="FollowStatus.Accepted"/> (default столбца).
    /// </summary>
    public FollowStatus Status { get; set; } = FollowStatus.Accepted;

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public User? FollowingUser { get; set; }
}
