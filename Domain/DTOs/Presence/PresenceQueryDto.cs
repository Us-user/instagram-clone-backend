namespace Domain.DTOs.Presence;

/// <summary>
/// Запрос статусов набора пользователей одним вызовом (§1) — удобно для списка чатов/подписчиков.
/// </summary>
public class PresenceQueryDto
{
    public List<string> UserIds { get; set; } = new();
}
