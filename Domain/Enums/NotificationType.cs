namespace Domain.Enums;

/// <summary>
/// Тип уведомления. Значения фиксированы контрактом (§2 новых фич) — новые типы
/// добавлять только в конец, чтобы не сдвинуть уже сохранённые в БД числовые коды.
/// </summary>
public enum NotificationType
{
    /// <summary>Лайк вашего поста.</summary>
    Like = 0,

    /// <summary>Новая подписка на вас (публичный аккаунт).</summary>
    Follow = 1,

    /// <summary>Комментарий к вашему посту.</summary>
    Comment = 2,

    /// <summary>Упоминание вас (@username) в посте/комменте/ответе на сторис.</summary>
    Mention = 3,

    /// <summary>Ответ на ваш комментарий.</summary>
    CommentReply = 4,

    /// <summary>Лайк вашего комментария.</summary>
    CommentLike = 5,

    /// <summary>Запрос на подписку (приватный аккаунт).</summary>
    FollowRequest = 6,

    /// <summary>Ваш запрос на подписку одобрен.</summary>
    FollowRequestAccepted = 7,

    /// <summary>Ответ на вашу сторис.</summary>
    StoryReply = 8,

    /// <summary>Ваш пост репостнули в сторис.</summary>
    PostShared = 9,

    /// <summary>Новый вход в аккаунт с ранее не встречавшегося устройства/IP (модуль сессий).</summary>
    NewLogin = 10
}
