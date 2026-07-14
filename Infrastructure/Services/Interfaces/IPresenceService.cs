using Domain.DTOs.Presence;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Онлайн-статусы (§1) со взаимной приватностью. Отдаёт <c>isOnline</c> + <c>lastSeen</c> с учётом
/// настройки <c>ShowOnlineStatus</c> обеих сторон и блокировок; также обрабатывает переходы
/// пользователя онлайн/офлайн (обновление <c>LastSeen</c> и real-time рассылка).
/// </summary>
public interface IPresenceService
{
    /// <summary>Статус одного пользователя (с учётом взаимной видимости). 404, если пользователь не найден.</summary>
    Task<Response<UserPresenceDto>> GetStatusAsync(string? userId);

    /// <summary>Статусы набора пользователей одним вызовом (несуществующие/скрытые — как офлайн).</summary>
    Task<Response<List<UserPresenceDto>>> GetStatusesAsync(IEnumerable<string>? userIds);

    /// <summary>Пользователь перешёл в онлайн (первое соединение): разослать статус заинтересованным.</summary>
    Task OnUserOnlineAsync(string userId);

    /// <summary>Пользователь ушёл в офлайн (последнее соединение): обновить <c>LastSeen</c> и разослать статус.</summary>
    Task OnUserOfflineAsync(string userId);
}
