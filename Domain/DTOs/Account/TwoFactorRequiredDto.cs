namespace Domain.DTOs.Account;

/// <summary>
/// Промежуточный ответ <c>/Account/login</c>, когда у пользователя включена 2FA (§11): вместо JWT
/// отдаётся признак «нужен второй фактор» и временный <see cref="TwoFactorToken"/>, который затем
/// передаётся в <c>/Account/login-2fa</c> (и в <c>/Account/send-2fa-email</c>).
/// </summary>
public class TwoFactorRequiredDto
{
    public bool RequiresTwoFactor { get; set; } = true;

    /// <summary>Временный токен сессии 2FA (не JWT; живёт ~10 минут).</summary>
    public string TwoFactorToken { get; set; } = string.Empty;

    /// <summary>Доступные способы второго фактора: <c>Totp</c>, <c>Email</c>, <c>Backup</c>.</summary>
    public List<string> Methods { get; set; } = new();
}
