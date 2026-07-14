namespace Domain.DTOs.Account;

/// <summary>
/// Результат <c>/Account/enable-2fa</c> (§11): секрет и данные для настройки приложения-аутентификатора,
/// плюс пачка одноразовых резервных кодов (показываются один раз). После сканирования QR пользователь
/// подтверждает включение первым валидным кодом через <c>/Account/confirm-2fa</c>.
/// </summary>
public class Enable2FaResultDto
{
    /// <summary>Секрет TOTP в Base32 (он же <see cref="ManualEntryKey"/> для ручного ввода).</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary><c>otpauth://</c>-URI для генерации QR-кода на клиенте.</summary>
    public string OtpauthUri { get; set; } = string.Empty;

    /// <summary>Ключ для ручного ввода в приложение-аутентификатор (равен <see cref="Secret"/>).</summary>
    public string ManualEntryKey { get; set; } = string.Empty;

    /// <summary>Одноразовые резервные коды (plaintext — сохранить пользователю; в БД только хэши).</summary>
    public List<string> BackupCodes { get; set; } = new();
}
