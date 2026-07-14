namespace Domain.DTOs.Presence;

/// <summary>
/// Событие «печатает…» в личном чате (§1). Эфемерное, в БД не сохраняется; клиент авто-сбрасывает
/// индикатор ~3 сек. <see cref="Kind"/> ∈ {<c>text</c>, <c>voice</c>} — обычный набор или запись голосового.
/// </summary>
public class TypingDto
{
    public int ChatId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    /// <summary>Тип набора: <c>text</c> (печатает) или <c>voice</c> (записывает голосовое).</summary>
    public string Kind { get; set; } = "text";
}
