using Domain.DTOs.Account;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Аутентификация и управление паролями: регистрация, вход, восстановление/смена пароля.
/// </summary>
public interface IAccountService
{
    /// <summary>Регистрирует пользователя и создаёт для него пустой профиль. Возвращает JWT.</summary>
    Task<Response<string>> RegisterAsync(RegisterDto dto);

    /// <summary>
    /// Проверяет учётные данные. Без 2FA — возвращает JWT-строку в <c>data</c> (контракт базы неизменен).
    /// При включённой 2FA — вместо токена отдаёт <see cref="TwoFactorRequiredDto"/> (§11).
    /// </summary>
    Task<Response<object>> LoginAsync(LoginDto dto);

    /// <summary>Завершает вход по второму фактору (TOTP/email/резервный код) и выдаёт JWT (§11).</summary>
    Task<Response<string>> LoginTwoFactorAsync(Login2FaDto dto);

    /// <summary>
    /// Включает 2FA для текущего пользователя: генерирует секрет + резервные коды, возвращает
    /// секрет/QR-URI и коды. Требует подтверждения через <see cref="ConfirmTwoFactorAsync"/> (§11).
    /// </summary>
    Task<Response<Enable2FaResultDto>> EnableTwoFactorAsync();

    /// <summary>Подтверждает включение 2FA первым валидным TOTP-кодом (§11).</summary>
    Task<Response<string>> ConfirmTwoFactorAsync(string? code);

    /// <summary>Отключает 2FA по валидному TOTP/резервному коду: чистит секрет и резервные коды (§11).</summary>
    Task<Response<string>> DisableTwoFactorAsync(string? code);

    /// <summary>Высылает email-код в рамках login-флоу (идентификация по <c>twoFactorToken</c>) (§11).</summary>
    Task<Response<string>> SendTwoFactorEmailAsync(Send2FaEmailDto dto);

    /// <summary>Перевыпускает пачку резервных кодов текущего пользователя (§11).</summary>
    Task<Response<List<string>>> RegenerateBackupCodesAsync();

    /// <summary>Генерирует токен сброса пароля (в учебных целях возвращается в ответе).</summary>
    Task<Response<string>> ForgotPasswordAsync(string? email);

    /// <summary>Сбрасывает пароль по ранее выданному токену.</summary>
    Task<Response<string>> ResetPasswordAsync(string? token, string? email, string? password, string? confirmPassword);

    /// <summary>Меняет пароль текущего пользователя (id берётся из claims).</summary>
    Task<Response<string>> ChangePasswordAsync(string? oldPassword, string? password, string? confirmPassword);
}
