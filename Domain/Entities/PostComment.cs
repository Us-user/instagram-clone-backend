namespace Domain.Entities;

/// <summary>Комментарий к посту. Может быть ответом на другой комментарий (макс. 2 уровня).</summary>
public class PostComment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Родительский комментарий верхнего уровня (Phase 14). null — комментарий верхнего уровня.
    /// Ответ на ответ прикрепляется к тому же родителю (третий уровень не создаётся).
    /// </summary>
    public int? ParentCommentId { get; set; }

    public Post? Post { get; set; }
    public User? User { get; set; }

    public PostComment? ParentComment { get; set; }
    public List<PostComment> Replies { get; set; } = new();
    public List<CommentLike> CommentLikes { get; set; } = new();
}
