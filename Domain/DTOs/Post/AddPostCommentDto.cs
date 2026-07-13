namespace Domain.DTOs.Post;

/// <summary>Добавление комментария к посту (или ответа на комментарий).</summary>
public class AddPostCommentDto
{
    public string Comment { get; set; } = string.Empty;
    public int PostId { get; set; }

    /// <summary>
    /// Необязательный id комментария, на который отвечаем (Phase 14). Ответ на ответ
    /// прикрепляется к родителю верхнего уровня — третий уровень не создаётся.
    /// </summary>
    public int? ParentCommentId { get; set; }
}
