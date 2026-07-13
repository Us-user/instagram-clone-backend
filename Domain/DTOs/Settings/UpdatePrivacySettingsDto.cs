using Domain.Enums;

namespace Domain.DTOs.Settings;

/// <summary>Обновление настроек приватности текущего пользователя (§6).</summary>
public class UpdatePrivacySettingsDto
{
    public bool IsPrivate { get; set; }
    public bool ShowOnlineStatus { get; set; }
    public WhoCanMessage WhoCanMessage { get; set; }
    public WhoCanMention WhoCanMention { get; set; }
    public WhoCanReplyStory WhoCanReplyStory { get; set; }
}
