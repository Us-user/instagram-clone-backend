namespace Domain.DTOs.Post;

/// <summary>Добавление комментария к посту.</summary>
public class AddPostCommentDto
{
    public string Comment { get; set; } = string.Empty;
    public int PostId { get; set; }
}
