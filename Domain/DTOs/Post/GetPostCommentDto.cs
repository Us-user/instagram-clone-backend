using Domain.DTOs.Mention;

namespace Domain.DTOs.Post;

/// <summary>Комментарий поста для чтения.</summary>
public class GetPostCommentDto
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserImage { get; set; }

    /// <summary>Id родительского комментария (Phase 14); null — комментарий верхнего уровня.</summary>
    public int? ParentCommentId { get; set; }

    /// <summary>Число ответов под этим комментарием (Phase 14). Для ответов всегда 0.</summary>
    public int RepliesCount { get; set; }

    /// <summary>Число лайков комментария (Phase 14).</summary>
    public int LikesCount { get; set; }

    /// <summary>Лайкнул ли комментарий текущий пользователь (Phase 14).</summary>
    public bool IsLiked { get; set; }

    /// <summary>Упомянутые (@username) в комментарии юзеры для кликабельных ссылок (Phase 13).</summary>
    public List<MentionedUserDto> MentionedUsers { get; set; } = new();
}
