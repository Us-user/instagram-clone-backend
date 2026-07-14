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

    /// <summary>
    /// Верифицирован ли аккаунт («синяя галочка»). Ставится/снимается администратором
    /// (Phase 19). Отдаётся во всех DTO пользователя/автора для отрисовки галочки.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Секрет TOTP для двухфакторной аутентификации (Phase 20, §11). Base32-строка, задаётся при
    /// <c>enable-2fa</c>, подтверждается первым валидным кодом (<c>confirm-2fa</c>), очищается при
    /// <c>disable-2fa</c>. Флаг включённости 2FA — стандартный <c>TwoFactorEnabled</c> из
    /// <see cref="IdentityUser{TKey}"/> (колонка уже есть в AspNetUsers).
    /// </summary>
    public string? TwoFactorSecret { get; set; }

    /// <summary>
    /// Приватный ли аккаунт (Phase 12). Быстрый флаг для проверок в лентах/подписках;
    /// источник истины — <see cref="PrivacySettings"/>, с которым синхронизируется.
    /// Новые подписки на приватный аккаунт идут через запрос (<see cref="Enums.FollowStatus.Pending"/>).
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Момент последнего онлайна (Phase 18, presence). Обновляется, когда пользователь
    /// отключает последнее real-time соединение (все хабы). <c>null</c> — ещё ни разу не был онлайн.
    /// Отдаётся вместе с <c>isOnline</c>; человекочитаемую строку «был(а) в сети» формирует клиент.
    /// Видимость статуса подчинена взаимной настройке <see cref="PrivacySettings.ShowOnlineStatus"/>.
    /// </summary>
    public DateTime? LastSeen { get; set; }

    // Навигации
    public UserProfile? UserProfile { get; set; }
    public PrivacySettings? PrivacySettings { get; set; }
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
