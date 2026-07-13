namespace Domain.DTOs.Mention;

/// <summary>
/// Упомянутый пользователь для отрисовки кликабельной ссылки на профиль (Phase 13).
/// Возвращается в DTO объектов, где есть упоминания (пост, комментарий, ответ на сторис).
/// </summary>
public class MentionedUserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
