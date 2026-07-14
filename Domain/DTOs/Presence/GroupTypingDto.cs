namespace Domain.DTOs.Presence;

/// <summary>
/// Событие «печатают…» в групповом чате (§1). В отличие от личного чата отдаётся весь актуальный
/// список печатающих (<see cref="Typers"/>), чтобы клиент показал «X и ещё N печатают…».
/// Список эфемерный, собирается в памяти сервера с коротким TTL; в БД не сохраняется.
/// </summary>
public class GroupTypingDto
{
    public int GroupChatId { get; set; }
    public List<TypingUserDto> Typers { get; set; } = new();
}
