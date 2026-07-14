using Domain.DTOs.CloseFriend;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Responses;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// «Близкие друзья» (§9). Владелец списка — текущий юзер (Id из claims). Добавлять можно любого
/// существующего пользователя, кроме себя и тех, с кем есть блокировка (в любую сторону).
/// Операции add/remove идемпотентны. Список <c>CloseFriends</c> определяет видимость
/// close-friends-сторис (фильтрация — в <see cref="StoryService"/>).
/// </summary>
public class CloseFriendService : ICloseFriendService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;

    public CloseFriendService(DataContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Response<string>> AddAsync(string? userId)
    {
        var currentId = _currentUser.GetRequiredUserId();

        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Не указан пользователь.");
        if (userId == currentId)
            throw new BadRequestException("Нельзя добавить себя в близкие друзья.");

        var exists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!exists)
            throw new NotFoundException("Пользователь не найден.");

        // Нельзя добавить в близкие при блокировке в любую сторону.
        if (await AccessGuard.IsBlockBetweenAsync(_context, currentId, userId))
            throw new ForbiddenException("Действие недоступно из-за блокировки.");

        var already = await _context.CloseFriends
            .AnyAsync(cf => cf.UserId == currentId && cf.FriendUserId == userId);
        if (already)
            return new Response<string>("Пользователь уже в близких друзьях.");

        _context.CloseFriends.Add(new CloseFriend
        {
            UserId = currentId,
            FriendUserId = userId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return new Response<string>("Пользователь добавлен в близкие друзья.");
    }

    public async Task<Response<string>> RemoveAsync(string? userId)
    {
        var currentId = _currentUser.GetRequiredUserId();

        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Не указан пользователь.");

        var link = await _context.CloseFriends
            .FirstOrDefaultAsync(cf => cf.UserId == currentId && cf.FriendUserId == userId);

        if (link is null)
            return new Response<string>("Пользователя нет в близких друзьях.");

        _context.CloseFriends.Remove(link);
        await _context.SaveChangesAsync();

        return new Response<string>("Пользователь убран из близких друзей.");
    }

    public async Task<PagedResponse<List<CloseFriendDto>>> GetListAsync(int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        var query = _context.CloseFriends.AsNoTracking()
            .Where(cf => cf.UserId == currentId)
            .OrderByDescending(cf => cf.CreatedAt);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(cf => new CloseFriendDto
            {
                UserId = cf.FriendUserId,
                UserName = cf.Friend!.UserName!,
                FullName = cf.Friend.FullName,
                Avatar = cf.Friend.Avatar,
                IsVerified = cf.Friend.IsVerified,
                CreatedAt = cf.CreatedAt
            })
            .ToListAsync();

        return new PagedResponse<List<CloseFriendDto>>(items, total, page, size);
    }
}
