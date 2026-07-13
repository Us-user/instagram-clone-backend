namespace Domain.Enums;

/// <summary>Кто может отвечать на сторис пользователя (настройка приватности, §6).</summary>
public enum WhoCanReplyStory
{
    /// <summary>Кто угодно.</summary>
    Everyone = 0,

    /// <summary>Только подписчики (одобренные).</summary>
    Followers = 1,

    /// <summary>Только «близкие друзья».</summary>
    CloseFriends = 2,

    /// <summary>Никто.</summary>
    Nobody = 3
}
