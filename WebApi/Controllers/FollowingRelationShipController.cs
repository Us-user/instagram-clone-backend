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

    /// <summary>Входящие запросы на подписку (Pending) к текущему пользователю (приватный аккаунт).</summary>
    [HttpGet("get-follow-requests")]
    public async Task<IActionResult> GetFollowRequests(
        [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var result = await _service.GetFollowRequestsAsync(pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Принять запрос на подписку от requesterUserId (+ уведомление об одобрении).</summary>
    [HttpPost("accept-request")]
    public async Task<IActionResult> AcceptRequest([FromQuery] string? requesterUserId)
    {
        var result = await _service.AcceptRequestAsync(requesterUserId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Отклонить запрос на подписку от requesterUserId.</summary>
    [HttpPost("decline-request")]
    public async Task<IActionResult> DeclineRequest([FromQuery] string? requesterUserId)
    {
        var result = await _service.DeclineRequestAsync(requesterUserId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Отменить свой исходящий запрос на подписку к followingUserId.</summary>
    [HttpDelete("cancel-request")]
    public async Task<IActionResult> CancelRequest([FromQuery] string? followingUserId)
    {
        var result = await _service.CancelRequestAsync(followingUserId);
        return StatusCode(result.StatusCode, result);
    }
}
