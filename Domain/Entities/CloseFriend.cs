namespace Domain.Entities;

/// <summary>
/// «Близкий друг» (§9): <see cref="UserId"/> добавил <see cref="FriendUserId"/> в свой список
/// близких. Направленная связь (не взаимная): близкие видят <see cref="Enums.StoryAudience.CloseFriends"/>
/// сторис владельца списка. Пара <c>(UserId, FriendUserId)</c> уникальна.
/// </summary>
public class CloseFriend
{
    public int Id { get; set; }

    /// <summary>Владелец списка близких друзей (FK на AspNetUsers).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Добавленный в близкие (FK на AspNetUsers).</summary>
    public string FriendUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public User? Friend { get; set; }
}
