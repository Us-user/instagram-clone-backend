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

    /// <summary>Подписан ли текущий пользователь на этот профиль.</summary>
    public bool IsFollowing { get; set; }
}
