using Domain.Enums;

namespace Domain.DTOs.UserProfile;

/// <summary>Обновление профиля текущего пользователя.</summary>
public class UpdateUserProfileDto
{
    public string About { get; set; } = string.Empty;
    public Gender Gender { get; set; }
}
