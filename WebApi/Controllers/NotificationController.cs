using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Уведомления текущего пользователя. Лента отдаётся сгруппированной («X, Y и ещё N…»);
/// новые уведомления дополнительно пушатся в реальном времени через SignalR-хаб
/// <c>/notificationHub</c> (событие <c>ReceiveNotification</c>). Id текущего юзера — из claims.
/// </summary>
[ApiController]
[Route("[controller]")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService) =>
        _notificationService = notificationService;

    /// <summary>Сгруппированная лента уведомлений с пагинацией.</summary>
    [HttpGet("get-notifications")]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var result = await _notificationService.GetNotificationsAsync(pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Число непрочитанных уведомлений (для «звоночка»).</summary>
    [HttpGet("get-unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var result = await _notificationService.GetUnreadCountAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Отметить одно уведомление прочитанным — только своё.</summary>
    [HttpPut("mark-as-read")]
    public async Task<IActionResult> MarkAsRead([FromQuery] int? id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Отметить все уведомления прочитанными.</summary>
    [HttpPut("mark-all-as-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var result = await _notificationService.MarkAllAsReadAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить одно уведомление — только своё.</summary>
    [HttpDelete("delete-notification")]
    public async Task<IActionResult> DeleteNotification([FromQuery] int? id)
    {
        var result = await _notificationService.DeleteNotificationAsync(id);
        return StatusCode(result.StatusCode, result);
    }
}
