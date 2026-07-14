using Infrastructure.Data.Seed;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Административные эндпоинты (§10): верификация («синяя галочка») и управление ролью Admin.
/// Все методы доступны только роли Admin. Целевой пользователь — из query-параметра <c>userId</c>;
/// текущий администратор — из JWT claims. Пути/методы/параметры воспроизводят контракт дословно.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class AdminController : ControllerBase
{
    private readonly IAdminService _service;

    public AdminController(IAdminService service) => _service = service;

    /// <summary>Поставить пользователю верификацию (синюю галочку).</summary>
    [HttpPost("verify-user")]
    public async Task<IActionResult> VerifyUser([FromQuery] string? userId)
    {
        var result = await _service.VerifyUserAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Снять верификацию с пользователя.</summary>
    [HttpDelete("unverify-user")]
    public async Task<IActionResult> UnverifyUser([FromQuery] string? userId)
    {
        var result = await _service.UnverifyUserAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Выдать пользователю роль Admin.</summary>
    [HttpPost("grant-admin")]
    public async Task<IActionResult> GrantAdmin([FromQuery] string? userId)
    {
        var result = await _service.GrantAdminAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Снять с пользователя роль Admin.</summary>
    [HttpDelete("revoke-admin")]
    public async Task<IActionResult> RevokeAdmin([FromQuery] string? userId)
    {
        var result = await _service.RevokeAdminAsync(userId);
        return StatusCode(result.StatusCode, result);
    }
}
