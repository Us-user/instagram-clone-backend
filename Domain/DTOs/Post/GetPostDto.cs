using Domain.DTOs.Mention;

namespace Domain.DTOs.Post;

/// <summary>Пост для чтения: поля + счётчики + флаги текущего юзера.</summary>
public class GetPostDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsReel { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserImage { get; set; }

    /// <summary>Верифицирован ли автор поста («синяя галочка», §10).</summary>
    public bool IsVerified { get; set; }

    public List<string> Images { get; set; } = new();

    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int ViewCount { get; set; }

    /// <summary>Лайкнул ли текущий пользователь.</summary>
    public bool IsLiked { get; set; }

    /// <summary>В избранном ли у текущего пользователя.</summary>
    public bool IsFavorite { get; set; }

    /// <summary>Упомянутые (@username) в посте юзеры для кликабельных ссылок (Phase 13).</summary>
    public List<MentionedUserDto> MentionedUsers { get; set; } = new();
}
