using Domain.DTOs.Notification;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Responses;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Уведомления: создание (с пушем через SignalR и правилом «не себе»), сгруппированная
/// лента, счётчик непрочитанных, отметка прочитанным (одно/все) и удаление. Группировка
/// одинаковых уведомлений на один объект выполняется в памяти по ключу
/// <c>(Type, EntityType, EntityId)</c> в пределах окна <see cref="GroupWindow"/>.
/// Id текущего юзера — из claims.
/// </summary>
public class NotificationService : INotificationService
{
    /// <summary>Окно группировки: уведомления одного типа на один объект в его пределах — одна группа.</summary>
    private static readonly TimeSpan GroupWindow = TimeSpan.FromHours(24);

    /// <summary>Сколько инициаторов показывать в группе (остальные — счётчиком).</summary>
    private const int MaxActorsPreview = 3;

    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationNotifier _notifier;

    public NotificationService(
        DataContext context,
        ICurrentUserService currentUser,
        INotificationNotifier notifier)
    {
        _context = context;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task<PagedResponse<List<GetNotificationDto>>> GetNotificationsAsync(
        int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        // Тянем плоский список (свежие сверху) с данными инициатора, группируем в памяти.
        var rows = await _context.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == currentId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationRow(
                n.Id,
                n.Type,
                n.EntityType,
                n.EntityId,
                n.IsRead,
                n.CreatedAt,
                n.ActorUserId,
                n.Actor!.UserName!,
                n.Actor.Avatar,
                n.Actor.IsVerified))
            .ToListAsync();

        var groups = GroupNotifications(rows);

        var pageItems = groups
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return new PagedResponse<List<GetNotificationDto>>(pageItems, groups.Count, page, size);
    }

    public async Task<Response<int>> GetUnreadCountAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();

        var count = await _context.Notifications
            .CountAsync(n => n.RecipientUserId == currentId && !n.IsRead);

        return new Response<int>(count);
    }

    public async Task<Response<bool>> MarkAsReadAsync(int? id)
    {
        if (id is null or <= 0)
            throw new BadRequestException("Некорректный Id уведомления.");

        var currentId = _currentUser.GetRequiredUserId();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id)
            ?? throw new NotFoundException("Уведомление не найдено.");

        if (notification.RecipientUserId != currentId)
            throw new ForbiddenException("Нет доступа к этому уведомлению.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> MarkAllAsReadAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();

        var unread = await _context.Notifications
            .Where(n => n.RecipientUserId == currentId && !n.IsRead)
            .ToListAsync();

        if (unread.Count > 0)
        {
            unread.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();
        }

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> DeleteNotificationAsync(int? id)
    {
        if (id is null or <= 0)
            throw new BadRequestException("Некорректный Id уведомления.");

        var currentId = _currentUser.GetRequiredUserId();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id)
            ?? throw new NotFoundException("Уведомление не найдено.");

        if (notification.RecipientUserId != currentId)
            throw new ForbiddenException("Нет доступа к этому уведомлению.");

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        return new Response<bool>(true);
    }

    public async Task CreateAsync(
        string recipientUserId,
        string actorUserId,
        NotificationType type,
        NotificationEntityType entityType,
        int? entityId)
    {
        // Правило «не себе»: собственные действия уведомлений не создают.
        if (string.IsNullOrWhiteSpace(recipientUserId)
            || string.IsNullOrWhiteSpace(actorUserId)
            || recipientUserId == actorUserId)
            return;

        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            ActorUserId = actorUserId,
            Type = type,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Инициатор для real-time payload (одиночная группа из одного актёра).
        var actor = await _context.Users.AsNoTracking()
            .Where(u => u.Id == actorUserId)
            .Select(u => new NotificationActorDto
            {
                Id = u.Id,
                UserName = u.UserName!,
                Avatar = u.Avatar,
                IsVerified = u.IsVerified
            })
            .FirstOrDefaultAsync();

        if (actor is null)
            return;

        var dto = new GetNotificationDto
        {
            Id = notification.Id,
            Type = notification.Type,
            EntityType = notification.EntityType,
            EntityId = notification.EntityId,
            Actors = new List<NotificationActorDto> { actor },
            ActorsCount = 1,
            IsRead = false,
            CreatedAt = notification.CreatedAt
        };

        await _notifier.NotifyAsync(recipientUserId, dto);
    }

    public async Task CreateNewLoginNotificationAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        // Уведомление «о себе»: получатель и инициатор — один и тот же пользователь, поэтому правило
        // «не себе» здесь намеренно не применяется. Ссылается на профиль (EntityType.User), без EntityId.
        var notification = new Notification
        {
            RecipientUserId = userId,
            ActorUserId = userId,
            Type = NotificationType.NewLogin,
            EntityType = NotificationEntityType.User,
            EntityId = null,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var actor = await _context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new NotificationActorDto
            {
                Id = u.Id,
                UserName = u.UserName!,
                Avatar = u.Avatar,
                IsVerified = u.IsVerified
            })
            .FirstOrDefaultAsync();

        if (actor is null)
            return;

        var dto = new GetNotificationDto
        {
            Id = notification.Id,
            Type = notification.Type,
            EntityType = notification.EntityType,
            EntityId = notification.EntityId,
            Actors = new List<NotificationActorDto> { actor },
            ActorsCount = 1,
            IsRead = false,
            CreatedAt = notification.CreatedAt
        };

        await _notifier.NotifyAsync(userId, dto);
    }

    /// <summary>
    /// Схлопывает плоский список (упорядочен по <c>CreatedAt</c> убыванию) в группы:
    /// подряд идущие уведомления с одинаковым ключом <c>(Type, EntityType, EntityId)</c>,
    /// укладывающиеся в окно <see cref="GroupWindow"/> от самого свежего в группе,
    /// объединяются. Инициаторы дедуплицируются; в превью — до <see cref="MaxActorsPreview"/>.
    /// </summary>
    private static List<GetNotificationDto> GroupNotifications(List<NotificationRow> rows)
    {
        var result = new List<GetNotificationDto>();
        // Открытая (незакрытая по времени) группа для каждого ключа.
        var open = new Dictionary<(NotificationType, NotificationEntityType, int?), GroupAccumulator>();

        foreach (var row in rows)
        {
            var key = (row.Type, row.EntityType, row.EntityId);

            if (open.TryGetValue(key, out var acc)
                && acc.LatestCreatedAt - row.CreatedAt <= GroupWindow)
            {
                acc.Add(row);
            }
            else
            {
                acc = new GroupAccumulator(row);
                open[key] = acc;
                result.Add(acc.Dto);
            }
        }

        // Финализируем превью актёров и счётчики (порядок result уже по убыванию времени).
        foreach (var dto in result)
            dto.Actors = dto.Actors.Take(MaxActorsPreview).ToList();

        return result;
    }

    /// <summary>Плоская проекция уведомления с данными инициатора для группировки в памяти.</summary>
    private sealed record NotificationRow(
        int Id,
        NotificationType Type,
        NotificationEntityType EntityType,
        int? EntityId,
        bool IsRead,
        DateTime CreatedAt,
        string ActorId,
        string ActorUserName,
        string? ActorAvatar,
        bool ActorIsVerified);

    /// <summary>Накопитель одной группы: держит DTO, множество актёров и время последнего.</summary>
    private sealed class GroupAccumulator
    {
        private readonly HashSet<string> _actorIds = new();

        public GroupAccumulator(NotificationRow first)
        {
            // Первый (самый свежий) уведомление задаёт представителя группы.
            Dto = new GetNotificationDto
            {
                Id = first.Id,
                Type = first.Type,
                EntityType = first.EntityType,
                EntityId = first.EntityId,
                IsRead = first.IsRead,
                CreatedAt = first.CreatedAt,
                Actors = new List<NotificationActorDto>(),
                ActorsCount = 0
            };
            LatestCreatedAt = first.CreatedAt;
            Add(first);
        }

        public GetNotificationDto Dto { get; }
        public DateTime LatestCreatedAt { get; }

        public void Add(NotificationRow row)
        {
            // Группа прочитана только когда прочитаны все её уведомления.
            if (!row.IsRead)
                Dto.IsRead = false;

            // Уникальные инициаторы; порядок добавления — по убыванию времени (свежие первыми).
            if (_actorIds.Add(row.ActorId))
            {
                Dto.Actors.Add(new NotificationActorDto
                {
                    Id = row.ActorId,
                    UserName = row.ActorUserName,
                    Avatar = row.ActorAvatar,
                    IsVerified = row.ActorIsVerified
                });
                Dto.ActorsCount = _actorIds.Count;
            }
        }
    }
}
