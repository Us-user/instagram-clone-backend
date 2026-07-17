namespace Domain.Enums;

/// <summary>
/// Аудитория эфира. <see cref="All"/> — все, кому виден контент хоста (с учётом приватности/блокировок);
/// <see cref="CloseFriends"/> — только близкие друзья хоста (§9 расширения).
/// </summary>
public enum LiveStreamAudience
{
    /// <summary>Все (в рамках приватности/блокировок).</summary>
    All = 0,

    /// <summary>Только близкие друзья хоста.</summary>
    CloseFriends = 1
}
