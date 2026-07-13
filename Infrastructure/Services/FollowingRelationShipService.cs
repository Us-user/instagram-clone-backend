using Domain.DTOs.User;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Подписки: список подписчиков/подписок, подписка и отписка текущего пользователя.
/// Дубли и подписка на себя запрещены; уникальность пары также защищена индексом БД.
/// </summary>
public class FollowingRelationShipService : IFollowingRelationShipService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;

    public FollowingRelationShipService(DataContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Response<List<GetUserDto>>> GetSubscribersAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Id пользователя обязателен.");

        // Подписчики = те, у кого FollowingUserId == userId; возвращаем их (User — подписчик).
        var subscribers = await _context.FollowingRelationShips.AsNoTracking()
            .Where(f => f.FollowingUserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new GetUserDto
            {
                Id = f.User!.Id,
                UserName = f.User.UserName!,
                Email = f.User.Email!,
                FullName = f.User.FullName,
                Avatar = f.User.Avatar
            })
            .ToListAsync();

        return new Response<List<GetUserDto>>(subscribers);
    }

    public async Task<Response<List<GetUserDto>>> GetSubscriptionsAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Id пользователя обязателен.");

        // Подписки = те, на кого userId подписан; возвращаем FollowingUser.
        var subscriptions = await _context.FollowingRelationShips.AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new GetUserDto
            {
                Id = f.FollowingUser!.Id,
                UserName = f.FollowingUser.UserName!,
                Email = f.FollowingUser.Email!,
                FullName = f.FollowingUser.FullName,
                Avatar = f.FollowingUser.Avatar
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

        var targetExists = await _context.Users.AnyAsync(u => u.Id == followingUserId);
        if (!targetExists)
            throw new NotFoundException("Пользователь, на которого вы подписываетесь, не найден.");

        var alreadyFollowing = await _context.FollowingRelationShips
            .AnyAsync(f => f.UserId == currentId && f.FollowingUserId == followingUserId);
        if (alreadyFollowing)
            throw new BadRequestException("Вы уже подписаны на этого пользователя.");

        _context.FollowingRelationShips.Add(new FollowingRelationShip
        {
            UserId = currentId,
            FollowingUserId = followingUserId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

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
}
