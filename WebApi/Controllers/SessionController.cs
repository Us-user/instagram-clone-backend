using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Активные сессии текущего пользователя (модуль «активные сеансы + refresh»). Позволяет посмотреть
/// список устройств и завершить конкретную/все прочие сессии. Id текущего юзера/сессии — из claims.
/// </summary>
[ApiController]
[Route("[controller]")]
public class SessionController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionController(ISessionService sessionService) => _sessionService = sessionService;

    /// <summary>Список активных сессий: текущая первой, далее по последней активности убыв.</summary>
    [HttpGet("get-active-sessions")]
    public async Task<IActionResult> GetActiveSessions()
    {
        var result = await _sessionService.GetActiveSessionsAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Завершить конкретную сессию (только свою).</summary>
    [HttpDelete("revoke-session")]
    public async Task<IActionResult> RevokeSession([FromQuery] Guid? sessionId)
    {
        var result = await _sessionService.RevokeSessionAsync(sessionId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>«Выйти на всех устройствах, кроме этого»: отзывает все сессии, кроме текущей.</summary>
    [HttpDelete("revoke-all-others")]
    public async Task<IActionResult> RevokeAllOthers()
    {
        var result = await _sessionService.RevokeAllOthersAsync();
        return StatusCode(result.StatusCode, result);
    }
}
