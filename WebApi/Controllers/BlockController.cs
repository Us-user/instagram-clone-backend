using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Блокировки пользователей (§6). Тот, кто блокирует/разблокирует, — текущий юзер (из claims).
/// При блокировке обе стороны отписываются друг от друга; контент/директ становятся взаимно
/// невидимы. Пути/методы/параметры воспроизводят контракт дословно.
/// </summary>
[ApiController]
[Route("[controller]")]
public class BlockController : ControllerBase
{
    private readonly IBlockService _service;

    public BlockController(IBlockService service) => _service = service;

    /// <summary>Заблокировать пользователя (+ взаимная отписка).</summary>
    [HttpPost("block-user")]
    public async Task<IActionResult> BlockUser([FromQuery] string? userId)
    {
        var result = await _service.BlockUserAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Снять блокировку с пользователя.</summary>
    [HttpDelete("unblock-user")]
    public async Task<IActionResult> UnblockUser([FromQuery] string? userId)
    {
        var result = await _service.UnblockUserAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Список заблокированных текущим пользователем (с пагинацией).</summary>
    [HttpGet("get-blocked-users")]
    public async Task<IActionResult> GetBlockedUsers(
        [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var result = await _service.GetBlockedUsersAsync(pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }
}
