namespace Domain.DTOs.Story;

/// <summary>Тело ответа на сторис (§9): <c>POST /Story/reply?storyId</c> — <c>{ text }</c>.</summary>
public class StoryReplyRequestDto
{
    /// <summary>Текст ответа. Уходит в директ автора сторис как личное сообщение.</summary>
    public string Text { get; set; } = string.Empty;
}
