using Domain.DTOs.Presence;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Онлайн-статусы пользователей (§1) со взаимной приватностью. Отдаёт <c>isOnline</c> + <c>lastSeen</c>;
/// если текущий юзер скрыл свой статус — статусы других он не видит (и его статус скрыт для всех).
/// Real-time обновления приходят через <c>/presenceHub</c> (событие <c>ReceivePresence</c>).
/// Id текущего юзера — из claims. Пути/методы/параметры воспроизводят контракт дословно.
/// </summary>
[ApiController]
[Route("[controller]")]
public class PresenceController : ControllerBase
{
    private readonly IPresenceService _presenceService;

    public PresenceController(IPresenceService presenceService) => _presenceService = presenceService;

    /// <summary>Онлайн-статус одного пользователя (с учётом взаимной видимости).</summary>
    [HttpGet("get-status")]
    public async Task<IActionResult> GetStatus([FromQuery] string userId)
    {
        var result = await _presenceService.GetStatusAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Онлайн-статусы набора пользователей одним запросом (для списка чатов/подписчиков).</summary>
    [HttpPost("get-statuses")]
    public async Task<IActionResult> GetStatuses([FromBody] PresenceQueryDto dto)
    {
        var result = await _presenceService.GetStatusesAsync(dto?.UserIds);
        return StatusCode(result.StatusCode, result);
    }
}
