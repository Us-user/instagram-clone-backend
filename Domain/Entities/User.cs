using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

/// <summary>
/// Пользователь системы. Расширяет <see cref="IdentityUser{TKey}"/> (ключ — строка).
/// </summary>
public class User : IdentityUser<string>
{
    /// <summary>
    /// Generic <see cref="IdentityUser{TKey}"/> (в отличие от не-generic <c>IdentityUser</c>)
    /// не генерирует строковый <c>Id</c> сам, а EF строковый ключ автоматически не заполняет.
    /// Поэтому задаём Id/SecurityStamp в конструкторе — иначе создание пользователя падает
    /// с «primary key property 'Id' is null». При чтении из БД EF перезаписывает эти значения.
    /// </summary>
    public User()
    {
        Id = Guid.NewGuid().ToString();
        SecurityStamp = Guid.NewGuid().ToString();
    }

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
