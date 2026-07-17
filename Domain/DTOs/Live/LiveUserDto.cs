namespace Domain.DTOs.Live;

/// <summary>
/// Краткая карточка пользователя в контексте эфира (хост, комментатор, зритель, гость). Отдаётся
/// в DTO и в real-time событиях. <see cref="IsVerified"/> — инвариант: галочку отдаём везде.
/// </summary>
public class LiveUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public bool IsVerified { get; set; }
}
