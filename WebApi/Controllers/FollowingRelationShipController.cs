using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Социальный граф подписок. Тот, кто подписывается/отписывается, — текущий юзер (из claims).
/// Пути/методы/параметры воспроизводят контракт дословно.
/// </summary>
[ApiController]
[Route("[controller]")]
public class FollowingRelationShipController : ControllerBase
{
    private readonly IFollowingRelationShipService _service;

    public FollowingRelationShipController(IFollowingRelationShipService service)
        => _service = service;

    /// <summary>Подписчики пользователя (кто подписан на UserId).</summary>
    [HttpGet("get-subscribers")]
    public async Task<IActionResult> GetSubscribers([FromQuery] string? userId)
    {
        var result = await _service.GetSubscribersAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Подписки пользователя (на кого подписан UserId).</summary>
    [HttpGet("get-subscriptions")]
    public async Task<IActionResult> GetSubscriptions([FromQuery] string? userId)
    {
        var result = await _service.GetSubscriptionsAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Текущий юзер подписывается на followingUserId. Запрет дубля и подписки на себя.</summary>
    [HttpPost("add-following-relation-ship")]
    public async Task<IActionResult> AddFollowingRelationShip([FromQuery] string? followingUserId)
    {
        var result = await _service.AddAsync(followingUserId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Текущий юзер отписывается от followingUserId.</summary>
    [HttpDelete("delete-following-relation-ship")]
    public async Task<IActionResult> DeleteFollowingRelationShip([FromQuery] string? followingUserId)
    {
        var result = await _service.DeleteAsync(followingUserId);
        return StatusCode(result.StatusCode, result);
    }
}
