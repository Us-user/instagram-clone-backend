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

    /// <summary>Вход: возвращает JWT в <c>data</c>.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _accountService.LoginAsync(dto);
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
