namespace Domain.DTOs.Live;

/// <summary>Тело запроса комментария в эфире: <c>POST /Live/add-comment?streamId</c> — <c>{ text }</c>.</summary>
public class AddLiveCommentDto
{
    public string Text { get; set; } = string.Empty;
}
