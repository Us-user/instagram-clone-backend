using Domain.Enums;

namespace Domain.Entities;

/// <summary>Профиль пользователя (1:1 с <see cref="User"/>).</summary>
public class UserProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? About { get; set; }
    public Gender Gender { get; set; }

    /// <summary>Имя файла изображения профиля в wwwroot/images (nullable).</summary>
    public string? Image { get; set; }

    public User? User { get; set; }
}
