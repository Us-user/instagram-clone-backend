using Domain.DTOs.FollowingRelationShip;
using Domain.DTOs.User;
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
/// Подписки: списки подписчиков/подписок (только одобренные), подписка/отписка текущего юзера,
/// а также запросы на подписку для приватных аккаунтов (Phase 12). Дубли и подписка на себя
/// запрещены; уникальность пары защищена индексом БД; блокировки учитываются через
/// <see cref="AccessGuard"/>. Id текущего юзера — из claims.
/// </summary>
public class FollowingRelationShipService : IFollowingRelationShipService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public FollowingRelationShipService(
        DataContext context,
        ICurrentUserService currentUser,
        INotificationService notifications)
    {
        _context = context;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<Response<List<GetUserDto>>> GetSubscribersAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Id пользователя обязателен.");

        var currentId = _currentUser.GetRequiredUserId();
        await EnsureCanViewListsAsync(userId, currentId);

        // Подписчики = одобренные связи, где FollowingUserId == userId; возвращаем подписчика (User).
        var subscribers = await _context.FollowingRelationShips.AsNoTracking()
            .Where(f => f.FollowingUserId == userId && f.Status == FollowStatus.Accepted)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new GetUserDto
            {
                Id = f.User!.Id,
                UserName = f.User.UserName!,
                Email = f.User.Email!,
                FullName = f.User.FullName,
                Avatar = f.User.Avatar,
                IsVerified = f.User.IsVerified
            })
            .ToListAsync();

        return new Response<List<GetUserDto>>(subscribers);
    }

    public async Task<Response<List<GetUserDto>>> GetSubscriptionsAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Id пользователя обязателен.");

        var currentId = _currentUser.GetRequiredUserId();
        await EnsureCanViewListsAsync(userId, currentId);

        // Подписки = одобренные связи, где userId — подписчик; возвращаем FollowingUser.
        var subscriptions = await _context.FollowingRelationShips.AsNoTracking()
            .Where(f => f.UserId == userId && f.Status == FollowStatus.Accepted)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new GetUserDto
            {
                Id = f.FollowingUser!.Id,
                UserName = f.FollowingUser.UserName!,
                Email = f.FollowingUser.Email!,
                FullName = f.FollowingUser.FullName,
                Avatar = f.FollowingUser.Avatar,
                IsVerified = f.FollowingUser.IsVerified
            })
            .ToListAsync();

        return new Response<List<GetUserDto>>(subscriptions);
    }

    public async Task<Response<bool>> AddAsync(string? followingUserId)
    {
        if (string.IsNullOrWhiteSpace(followingUserId))
            throw new BadRequestException("Id пользователя обязателен.");

        var currentId = _currentUser.GetRequiredUserId();

        if (followingUserId == currentId)
            throw new BadRequestException("Нельзя подписаться на самого себя.");

        var target = await _context.Users.AsNoTracking()
            .Where(u => u.Id == followingUserId)
            .Select(u => new { u.Id, u.IsPrivate })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Пользователь, на которого вы подписываетесь, не найден.");

        if (await AccessGuard.IsBlockBetweenAsync(_context, currentId, followingUserId))
            throw new ForbiddenException("Действие недоступно из-за блокировки.");

        var alreadyFollowing = await _context.FollowingRelationShips
            .AnyAsync(f => f.UserId == currentId && f.FollowingUserId == followingUserId);
        if (alreadyFollowing)
            throw new BadRequestException("Вы уже подписаны или уже отправили запрос этому пользователю.");

        // Публичный аккаунт → сразу одобрено; приватный → запрос на подписку.
        var status = target.IsPrivate ? FollowStatus.Pending : FollowStatus.Accepted;

        _context.FollowingRelationShips.Add(new FollowingRelationShip
        {
            UserId = currentId,
            FollowingUserId = followingUserId,
            Status = status,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Публичный → уведомление о новой подписке; приватный → уведомление-запрос.
        var type = status == FollowStatus.Accepted
            ? NotificationType.Follow
            : NotificationType.FollowRequest;

        await _notifications.CreateAsync(
            followingUserId, currentId, type, NotificationEntityType.User, null);

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> DeleteAsync(string? followingUserId)
    {
        if (string.IsNullOrWhiteSpace(followingUserId))
            throw new BadRequestException("Id пользователя обязателен.");

        var currentId = _currentUser.GetRequiredUserId();

        var relation = await _context.FollowingRelationShips
            .FirstOrDefaultAsync(f => f.UserId == currentId && f.FollowingUserId == followingUserId)
            ?? throw new NotFoundException("Вы не подписаны на этого пользователя.");

        _context.FollowingRelationShips.Remove(relation);
        await _context.SaveChangesAsync();

        return new Response<bool>(true);
    }

    public async Task<PagedResponse<List<GetFollowRequestDto>>> GetFollowRequestsAsync(
        int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        var query = _context.FollowingRelationShips.AsNoTracking()
            .Where(f => f.FollowingUserId == currentId && f.Status == FollowStatus.Pending)
            .OrderByDescending(f => f.CreatedAt);

        var total = await query.CountAsync();

        var requests = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(f => new GetFollowRequestDto
            {
                UserId = f.User!.Id,
                UserName = f.User.UserName!,
                FullName = f.User.FullName,
                Avatar = f.User.Avatar,
                IsVerified = f.User.IsVerified,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();

        return new PagedResponse<List<GetFollowRequestDto>>(requests, total, page, size);
    }

    public async Task<Response<bool>> AcceptRequestAsync(string? requesterUserId)
    {
        if (string.IsNullOrWhiteSpace(requesterUserId))
            throw new BadRequestException("Id пользователя обязателен.");

        var currentId = _currentUser.GetRequiredUserId();

        var request = await _context.FollowingRelationShips
            .FirstOrDefaultAsync(f =>
                f.UserId == requesterUserId &&
                f.FollowingUserId == currentId &&
                f.Status == FollowStatus.Pending)
            ?? throw new NotFoundException("Запрос на подписку не найден.");

        request.Status = FollowStatus.Accepted;
        await _context.SaveChangesAsync();

        // Уведомляем запросившего об одобрении.
        await _notifications.CreateAsync(
            requesterUserId, currentId,
            NotificationType.FollowRequestAccepted, NotificationEntityType.User, null);

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> DeclineRequestAsync(string? requesterUserId)
    {
        if (string.IsNullOrWhiteSpace(requesterUserId))
            throw new BadRequestException("Id пользователя обязателен.");

        var currentId = _currentUser.GetRequiredUserId();

        var request = await _context.FollowingRelationShips
            .FirstOrDefaultAsync(f =>
                f.UserId == requesterUserId &&
                f.FollowingUserId == currentId &&
                f.Status == FollowStatus.Pending)
            ?? throw new NotFoundException("Запрос на подписку не найден.");

        _context.FollowingRelationShips.Remove(request);
        await _context.SaveChangesAsync();

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> CancelRequestAsync(string? followingUserId)
    {
        if (string.IsNullOrWhiteSpace(followingUserId))
            throw new BadRequestException("Id пользователя обязателен.");

        var currentId = _currentUser.GetRequiredUserId();

        var request = await _context.FollowingRelationShips
            .FirstOrDefaultAsync(f =>
                f.UserId == currentId &&
                f.FollowingUserId == followingUserId &&
                f.Status == FollowStatus.Pending)
            ?? throw new NotFoundException("Исходящий запрос на подписку не найден.");

        _context.FollowingRelationShips.Remove(request);
        await _context.SaveChangesAsync();

        return new Response<bool>(true);
    }

    /// <summary>
    /// Проверяет, что <paramref name="currentId"/> вправе видеть списки подписок
    /// <paramref name="targetUserId"/>: у приватного чужого аккаунта списки скрыты без
    /// одобренной подписки; при блокировке — скрыты в любом случае.
    /// </summary>
    private async Task EnsureCanViewListsAsync(string targetUserId, string currentId)
    {
        var canView = await AccessGuard.CanViewContentAsync(_context, targetUserId, currentId);
        if (!canView)
            throw new ForbiddenException("Списки этого аккаунта скрыты.");
    }
}
