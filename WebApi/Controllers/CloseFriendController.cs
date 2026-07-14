using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// «Близкие друзья» (§9): добавить/убрать пользователя и получить список. Владелец списка —
/// текущий юзер (Id из claims). Список определяет, кто видит close-friends-сторис автора.
/// Пути/методы/параметры воспроизводят контракт дословно.
/// </summary>
[ApiController]
[Route("[controller]")]
public class CloseFriendController : ControllerBase
{
    private readonly ICloseFriendService _closeFriendService;

    public CloseFriendController(ICloseFriendService closeFriendService) =>
        _closeFriendService = closeFriendService;

    /// <summary>Добавить пользователя в близкие друзья.</summary>
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromQuery] string userId)
    {
        var result = await _closeFriendService.AddAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Убрать пользователя из близких друзей.</summary>
    [HttpDelete("remove")]
    public async Task<IActionResult> Remove([FromQuery] string userId)
    {
        var result = await _closeFriendService.RemoveAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Список близких друзей текущего юзера (с пагинацией).</summary>
    [HttpGet("get-list")]
    public async Task<IActionResult> GetList([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var result = await _closeFriendService.GetListAsync(pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }
}
