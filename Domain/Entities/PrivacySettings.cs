using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Настройки приватности пользователя (1:1 с <see cref="User"/>). Источник истины для
/// приватности: <see cref="IsPrivate"/> синхронизируется с <see cref="User.IsPrivate"/>
/// (быстрый флаг для проверок в лентах/подписках). Создаётся лениво при первом
/// обращении к <c>/Settings</c>; для не тронувших настройки действуют значения по умолчанию.
/// </summary>
public class PrivacySettings
{
    public int Id { get; set; }

    /// <summary>Владелец настроек (FK 1:1 на AspNetUsers).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Приватный ли аккаунт (дублирует <see cref="User.IsPrivate"/>).</summary>
    public bool IsPrivate { get; set; }

    /// <summary>Показывать ли онлайн-статус (используется в §1 presence).</summary>
    public bool ShowOnlineStatus { get; set; } = true;

    public WhoCanMessage WhoCanMessage { get; set; } = WhoCanMessage.Everyone;
    public WhoCanMention WhoCanMention { get; set; } = WhoCanMention.Everyone;
    public WhoCanReplyStory WhoCanReplyStory { get; set; } = WhoCanReplyStory.Everyone;

    public User? User { get; set; }
}
