namespace Domain.DTOs.Account;

/// <summary>
/// Завершение входа со вторым фактором (§11): <c>/Account/login-2fa</c>. По валидному коду выдаётся JWT.
/// </summary>
public class Login2FaDto
{
    /// <summary>Временный токен, полученный от <c>/Account/login</c>.</summary>
    public string TwoFactorToken { get; set; } = string.Empty;

    /// <summary>Код подтверждения (TOTP / email-код / резервный код — зависит от <see cref="Method"/>).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Способ подтверждения: <c>Totp</c> (по умолчанию), <c>Email</c> или <c>Backup</c> (без учёта регистра).</summary>
    public string Method { get; set; } = "Totp";
}
