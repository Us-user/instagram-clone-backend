namespace Domain.Enums;

/// <summary>
/// Аудитория сторис (§9). <see cref="CloseFriends"/>-сторис видны только тем, кто в списке
/// «близких друзей» автора; <see cref="All"/> — всем, кому доступен контент автора.
/// </summary>
public enum StoryAudience
{
    /// <summary>Все (по обычным правилам видимости — подписки/приватность/блокировки).</summary>
    All = 0,

    /// <summary>Только «близкие друзья» автора (<see cref="Entities.CloseFriend"/>).</summary>
    CloseFriends = 1
}
