using Domain.Enums;

namespace Domain.DTOs.Settings;

/// <summary>Настройки приватности текущего пользователя.</summary>
public class GetPrivacySettingsDto
{
    public bool IsPrivate { get; set; }
    public bool ShowOnlineStatus { get; set; }
    public WhoCanMessage WhoCanMessage { get; set; }
    public WhoCanMention WhoCanMention { get; set; }
    public WhoCanReplyStory WhoCanReplyStory { get; set; }
}
