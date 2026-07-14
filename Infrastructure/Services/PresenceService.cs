using Domain.DTOs.Presence;
using Domain.Exceptions;
using Domain.Responses;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Presence-сервис (§1): онлайн-статусы со <b>взаимной</b> приватностью и рассылка их изменений.
/// Взаимная логика <c>ShowOnlineStatus</c>: если запрашивающий скрыл свой статус, он не видит
/// чужие (все скрыты) и его статус скрыт для всех. Дополнительно статус скрыт при блокировке
/// в любую сторону и если цель сама выключила показ. Онлайн определяется по <see cref="IPresenceTracker"/>
/// (активные соединения), <c>lastSeen</c> — по <c>User.LastSeen</c>. Id текущего юзера — из claims.
/// </summary>
public class PresenceService : IPresenceService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPresenceTracker _tracker;
    private readonly IPresenceNotifier _notifier;

    public PresenceService(
        DataContext context,
        ICurrentUserService currentUser,
        IPresenceTracker tracker,
        IPresenceNotifier notifier)
    {
        _context = context;
        _currentUser = currentUser;
        _tracker = tracker;
        _notifier = notifier;
    }

    public async Task<Response<UserPresenceDto>> GetStatusAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Не указан пользователь.");

        var exists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!exists)
            throw new NotFoundException("Пользователь не найден.");

        var statuses = await BuildStatusesAsync(new List<string> { userId });
        return new Response<UserPresenceDto>(statuses[0]);
    }

    public async Task<Response<List<UserPresenceDto>>> GetStatusesAsync(IEnumerable<string>? userIds)
    {
        var ids = (userIds ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var statuses = await BuildStatusesAsync(ids);
        return new Response<List<UserPresenceDto>>(statuses);
    }

    public async Task OnUserOnlineAsync(string userId) => await BroadcastAsync(userId, isOnline: true, lastSeen: null);

    public async Task OnUserOfflineAsync(string userId)
    {
        var now = DateTime.UtcNow;

        // LastSeen пишем всегда (видимость гейтится на чтении); свои настройки не важны для записи.
        await _context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastSeen, now));

        await BroadcastAsync(userId, isOnline: false, lastSeen: now);
    }

    /// <summary>Собирает статусы для набора Id с учётом взаимной видимости относительно текущего юзера.</summary>
    private async Task<List<UserPresenceDto>> BuildStatusesAsync(List<string> ids)
    {
        var currentId = _currentUser.GetRequiredUserId();

        var result = new List<UserPresenceDto>(ids.Count);
        if (ids.Count == 0)
            return result;

        var currentShows = await ShowsOnlineStatusAsync(currentId);

        // Данные целей: LastSeen + их собственная настройка показа (нет строки настроек → показывают).
        var targets = await _context.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.LastSeen,
                Shows = u.PrivacySettings == null || u.PrivacySettings.ShowOnlineStatus
            })
            .ToDictionaryAsync(x => x.Id);

        var blockedSet = (await AccessGuard.BlockRelatedUserIds(_context, currentId).ToListAsync())
            .ToHashSet();

        foreach (var id in ids)
        {
            // Свой статус виден себе всегда.
            if (id == currentId)
            {
                var self = targets.GetValueOrDefault(id);
                result.Add(BuildDto(id, self?.LastSeen));
                continue;
            }

            // Запрашивающий скрыл свой статус → не видит чужие; блокировка/скрытие цели → скрыто.
            if (!currentShows
                || !targets.TryGetValue(id, out var t)
                || blockedSet.Contains(id)
                || !t.Shows)
            {
                result.Add(Hidden(id));
                continue;
            }

            result.Add(BuildDto(id, t.LastSeen));
        }

        return result;
    }

    /// <summary>Разослать статус пользователя тем, кому он виден (собеседники по личным чатам и группам).</summary>
    private async Task BroadcastAsync(string userId, bool isOnline, DateTime? lastSeen)
    {
        // Скрытый пользователь никому не транслирует свой статус.
        if (!await ShowsOnlineStatusAsync(userId))
            return;

        var viewers = await RelevantViewersAsync(userId);
        if (viewers.Count == 0)
            return;

        var dto = new UserPresenceDto
        {
            UserId = userId,
            IsOnline = isOnline,
            LastSeen = isOnline ? null : lastSeen
        };

        await _notifier.NotifyPresenceAsync(viewers, dto);
    }

    /// <summary>
    /// Заинтересованные наблюдатели статуса: собеседники по личным чатам ∪ участники общих групп,
    /// минус заблокированные (в любую сторону) и те, кто сам выключил показ статуса (взаимность).
    /// </summary>
    private async Task<List<string>> RelevantViewersAsync(string userId)
    {
        var chatPartners = _context.Chats
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Select(c => c.User1Id == userId ? c.User2Id : c.User1Id);

        var myGroupIds = _context.GroupChatMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupChatId);

        var coMembers = _context.GroupChatMembers
            .Where(m => myGroupIds.Contains(m.GroupChatId) && m.UserId != userId)
            .Select(m => m.UserId);

        var candidates = await chatPartners.Concat(coMembers)
            .Distinct()
            .ToListAsync();

        if (candidates.Count == 0)
            return candidates;

        var blocked = (await AccessGuard.BlockRelatedUserIds(_context, userId).ToListAsync()).ToHashSet();

        // Наблюдатели, выключившие показ статуса, не видят чужие статусы (взаимность) — исключаем.
        var hiddenViewers = (await _context.PrivacySettings.AsNoTracking()
                .Where(p => candidates.Contains(p.UserId) && !p.ShowOnlineStatus)
                .Select(p => p.UserId)
                .ToListAsync())
            .ToHashSet();

        return candidates
            .Where(id => !blocked.Contains(id) && !hiddenViewers.Contains(id))
            .ToList();
    }

    /// <summary>Показывает ли пользователь свой онлайн-статус (нет строки настроек → да, по умолчанию).</summary>
    private async Task<bool> ShowsOnlineStatusAsync(string userId)
    {
        var shows = await _context.PrivacySettings.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (bool?)p.ShowOnlineStatus)
            .FirstOrDefaultAsync();

        return shows ?? true;
    }

    private UserPresenceDto BuildDto(string userId, DateTime? lastSeen)
    {
        var online = _tracker.IsOnline(userId);
        return new UserPresenceDto
        {
            UserId = userId,
            IsOnline = online,
            LastSeen = online ? null : lastSeen
        };
    }

    private static UserPresenceDto Hidden(string userId) =>
        new() { UserId = userId, IsOnline = false, LastSeen = null };
}
