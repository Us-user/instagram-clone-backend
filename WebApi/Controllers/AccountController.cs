using Domain.DTOs.Account;
using Domain.Responses;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Аутентификация и управление паролями. Пути/методы/параметры воспроизводят контракт дословно.
/// </summary>
[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService) => _accountService = accountService;

    /// <summary>Регистрация: создаёт пользователя и пустой профиль, возвращает JWT.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _accountService.RegisterAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Вход. Без 2FA — возвращает JWT в <c>data</c>. При включённой 2FA — вместо токена отдаёт
    /// признак «нужен второй фактор» и временный <c>twoFactorToken</c> (§11).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _accountService.LoginAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Завершение входа со вторым фактором (TOTP/email/резервный код) → JWT (§11). Аноним.</summary>
    [HttpPost("login-2fa")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginTwoFactor([FromBody] Login2FaDto dto)
    {
        var result = await _accountService.LoginTwoFactorAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Высылает email-код в рамках login-флоу (пользователь ещё не аутентифицирован — идентификация
    /// по <c>twoFactorToken</c>). В учебных целях код возвращается в <c>data</c> (§11). Аноним.
    /// </summary>
    [HttpPost("send-2fa-email")]
    [AllowAnonymous]
    public async Task<IActionResult> SendTwoFactorEmail([FromBody] Send2FaEmailDto dto)
    {
        var result = await _accountService.SendTwoFactorEmailAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Включает 2FA для текущего пользователя: возвращает секрет/QR-URI и резервные коды.
    /// Требует подтверждения через <c>confirm-2fa</c> первым валидным кодом (§11).
    /// </summary>
    [HttpPost("enable-2fa")]
    public async Task<IActionResult> EnableTwoFactor()
    {
        var result = await _accountService.EnableTwoFactorAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Подтверждает включение 2FA первым валидным TOTP-кодом (§11).</summary>
    [HttpPost("confirm-2fa")]
    public async Task<IActionResult> ConfirmTwoFactor([FromQuery] string? code)
    {
        var result = await _accountService.ConfirmTwoFactorAsync(code);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Отключает 2FA по валидному TOTP или резервному коду (§11).</summary>
    [HttpPost("disable-2fa")]
    public async Task<IActionResult> DisableTwoFactor([FromQuery] string? code)
    {
        var result = await _accountService.DisableTwoFactorAsync(code);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Перевыпускает пачку одноразовых резервных кодов текущего пользователя (§11).</summary>
    [HttpPost("regenerate-backup-codes")]
    public async Task<IActionResult> RegenerateBackupCodes()
    {
        var result = await _accountService.RegenerateBackupCodesAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Генерирует токен сброса пароля по email (в учебных целях возвращается в ответе).
    /// Аноним: пользователь, забывший пароль, не может быть аутентифицирован.
    /// </summary>
    [HttpDelete("ForgotPassword")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromQuery] string? email)
    {
        var result = await _accountService.ForgotPasswordAsync(email);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Сбрасывает пароль по ранее выданному токену. Аноним (см. ForgotPassword).</summary>
    [HttpDelete("ResetPassword")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromQuery] string? token,
        [FromQuery] string? email,
        [FromQuery] string? password,
        [FromQuery] string? confirmPassword)
    {
        var result = await _accountService.ResetPasswordAsync(token, email, password, confirmPassword);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Меняет пароль текущего пользователя (id берётся из claims).</summary>
    [HttpPut("ChangePassword")]
    public async Task<IActionResult> ChangePassword(
        [FromQuery] string? oldPassword,
        [FromQuery] string? password,
        [FromQuery] string? confirmPassword)
    {
        var result = await _accountService.ChangePasswordAsync(oldPassword, password, confirmPassword);
        return StatusCode(result.StatusCode, result);
    }
}
