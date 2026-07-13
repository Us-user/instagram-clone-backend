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

    /// <summary>Упомянутые (@username) в комментарии юзеры для кликабельных ссылок (Phase 13).</summary>
    public List<MentionedUserDto> MentionedUsers { get; set; } = new();
}
