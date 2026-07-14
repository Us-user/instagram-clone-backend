namespace Domain.Entities;

/// <summary>
/// Одноразовый резервный код двухфакторной аутентификации (§11). Пачка кодов генерируется при
/// включении 2FA (и при регенерации); в БД хранится только хэш (<see cref="CodeHash"/>), сам код
/// показывается пользователю один раз. При успешном входе через резервный код запись помечается
/// <see cref="IsUsed"/> и повторно не принимается.
/// </summary>
public class BackupCode
{
    public int Id { get; set; }

    /// <summary>Владелец кода (FK на AspNetUsers).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Хэш кода (SHA-256, hex). Сам код в БД не хранится.</summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>Использован ли код (одноразовый).</summary>
    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
