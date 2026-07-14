namespace Domain.DTOs.Account;

/// <summary>
/// Запрос email-кода в рамках login-флоу (§11): <c>/Account/send-2fa-email</c>. Пользователь ещё не
/// аутентифицирован (JWT нет), поэтому идентифицируется по временному <see cref="TwoFactorToken"/>.
/// </summary>
public class Send2FaEmailDto
{
    /// <summary>Временный токен, полученный от <c>/Account/login</c>.</summary>
    public string TwoFactorToken { get; set; } = string.Empty;
}
