namespace Domain.Enums;

/// <summary>Кто может писать пользователю в директ (настройка приватности, §6).</summary>
public enum WhoCanMessage
{
    /// <summary>Кто угодно.</summary>
    Everyone = 0,

    /// <summary>Только подписчики (одобренные).</summary>
    Followers = 1,

    /// <summary>Никто.</summary>
    Nobody = 2
}
