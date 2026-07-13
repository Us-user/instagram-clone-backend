namespace Domain.Entities;

/// <summary>
/// Хэштег (Phase 13). Тег хранится нормализованным (нижний регистр, без символа <c>#</c>)
/// и уникален. <see cref="PostsCount"/> — денормализованный счётчик связанных постов
/// (инкремент при добавлении поста с тегом, декремент при удалении).
/// </summary>
public class Hashtag
{
    public int Id { get; set; }

    /// <summary>Нормализованный тег (unique, lowercase, без <c>#</c>).</summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>Число постов с этим тегом (денормализованный счётчик популярности).</summary>
    public int PostsCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<PostHashtag> PostHashtags { get; set; } = new();
}
