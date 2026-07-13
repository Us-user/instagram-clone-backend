using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

/// <summary>
/// Пользователь системы. Расширяет <see cref="IdentityUser{TKey}"/> (ключ — строка).
/// </summary>
public class User : IdentityUser<string>
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Имя файла аватара в wwwroot/images (nullable).</summary>
    public string? Avatar { get; set; }

    // Навигации
    public UserProfile? UserProfile { get; set; }
    public List<Post> Posts { get; set; } = new();
    public List<PostLike> PostLikes { get; set; } = new();
    public List<PostView> PostViews { get; set; } = new();
    public List<PostComment> PostComments { get; set; } = new();
    public List<PostFavorite> PostFavorites { get; set; } = new();
    public List<Story> Stories { get; set; } = new();
    public List<StoryLike> StoryLikes { get; set; } = new();
    public List<StoryView> StoryViews { get; set; } = new();
    public List<SearchHistory> SearchHistories { get; set; } = new();
}
