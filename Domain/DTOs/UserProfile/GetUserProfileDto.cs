using Domain.Enums;

namespace Domain.DTOs.UserProfile;

/// <summary>Профиль пользователя со счётчиками и флагом подписки текущего юзера.</summary>
public class GetUserProfileDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? About { get; set; }
    public Gender Gender { get; set; }
    public string? Image { get; set; }

    public int PostCount { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }

    /// <summary>Подписан ли текущий пользователь на этот профиль (одобренная подписка).</summary>
    public bool IsFollowing { get; set; }

    /// <summary>Приватный ли аккаунт (Phase 12). Для приватного чужого профиля контент скрыт.</summary>
    public bool IsPrivate { get; set; }

    /// <summary>Верифицирован ли пользователь («синяя галочка», §10).</summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Отправлен ли текущим пользователем запрос на подписку, ожидающий одобрения
    /// (кнопка «Запрос отправлен» для приватного профиля).
    /// </summary>
    public bool IsRequested { get; set; }
}
