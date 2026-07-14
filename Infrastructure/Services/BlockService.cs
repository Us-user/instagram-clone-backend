using Domain.DTOs.User;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Responses;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Блокировки: заблокировать/разблокировать пользователя и список заблокированных.
/// При блокировке удаляются обе связи подписки (в любом статусе) — профиль/контент/директ
/// становятся взаимно невидимы (проверки — через <see cref="AccessGuard"/>). Старые лайки
/// и комментарии не трогаем (§6). Id текущего юзера — из claims.
/// </summary>
public class BlockService : IBlockService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;

    public BlockService(DataContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Response<bool>> BlockUserAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Id пользователя обязателен.");

        var currentId = _currentUser.GetRequiredUserId();

        if (userId == currentId)
            throw new BadRequestException("Нельзя заблокировать самого себя.");

        var targetExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!targetExists)
            throw new NotFoundException("Пользователь не найден.");

        var alreadyBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerUserId == currentId && b.BlockedUserId == userId);
        if (alreadyBlocked)
            throw new BadRequestException("Пользователь уже заблокирован.");

        // Взаимная отписка: удаляем обе связи в любом статусе (включая pending-запросы).
        var relations = await _context.FollowingRelationShips
            .Where(f =>
                (f.UserId == currentId && f.FollowingUserId == userId) ||
                (f.UserId == userId && f.FollowingUserId == currentId))
            .ToListAsync();
        if (relations.Count > 0)
            _context.FollowingRelationShips.RemoveRange(relations);

        _context.Blocks.Add(new Block
        {
            BlockerUserId = currentId,
            BlockedUserId = userId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return new Response<bool>(true);
    }

    public async Task<Response<bool>> UnblockUserAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Id пользователя обязателен.");

        var currentId = _currentUser.GetRequiredUserId();

        var block = await _context.Blocks
            .FirstOrDefaultAsync(b => b.BlockerUserId == currentId && b.BlockedUserId == userId)
            ?? throw new NotFoundException("Этот пользователь не заблокирован.");

        _context.Blocks.Remove(block);
        await _context.SaveChangesAsync();

        return new Response<bool>(true);
    }

    public async Task<PagedResponse<List<GetUserDto>>> GetBlockedUsersAsync(int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        var query = _context.Blocks.AsNoTracking()
            .Where(b => b.BlockerUserId == currentId)
            .OrderByDescending(b => b.CreatedAt);

        var total = await query.CountAsync();

        var users = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(b => new GetUserDto
            {
                Id = b.BlockedUser!.Id,
                UserName = b.BlockedUser.UserName!,
                Email = b.BlockedUser.Email!,
                FullName = b.BlockedUser.FullName,
                Avatar = b.BlockedUser.Avatar,
                IsVerified = b.BlockedUser.IsVerified
            })
            .ToListAsync();

        return new PagedResponse<List<GetUserDto>>(users, total, page, size);
    }
}
