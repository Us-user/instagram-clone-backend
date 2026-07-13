namespace Domain.Entities;

/// <summary>
/// Блокировка: <see cref="BlockerUserId"/> заблокировал <see cref="BlockedUserId"/>.
/// При блокировке обе стороны отписываются друг от друга; профиль/контент/директ
/// становятся взаимно невидимы. Старые лайки и комментарии не удаляются (§6).
/// </summary>
public class Block
{
    public int Id { get; set; }

    /// <summary>Кто заблокировал.</summary>
    public string BlockerUserId { get; set; } = string.Empty;

    /// <summary>Кого заблокировали.</summary>
    public string BlockedUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public User? BlockerUser { get; set; }
    public User? BlockedUser { get; set; }
}
