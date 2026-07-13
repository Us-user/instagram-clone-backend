namespace Domain.Enums;

/// <summary>Кто может упоминать пользователя (@username) — настройка приватности, §6.</summary>
public enum WhoCanMention
{
    /// <summary>Кто угодно.</summary>
    Everyone = 0,

    /// <summary>Только подписчики (одобренные).</summary>
    Followers = 1,

    /// <summary>Никто.</summary>
    Nobody = 2
}
