namespace Domain.DTOs.Presence;

/// <summary>Один печатающий участник группы (элемент списка в <see cref="GroupTypingDto"/>).</summary>
public class TypingUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    /// <summary>Тип набора: <c>text</c> (печатает) или <c>voice</c> (записывает голосовое).</summary>
    public string Kind { get; set; } = "text";
}
