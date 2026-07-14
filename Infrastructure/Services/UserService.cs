using Domain.DTOs.User;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Responses;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Поиск пользователей, ведение истории поиска (текст и просмотренные профили)
/// и удаление пользователя. Все операции истории привязаны к текущему юзеру из claims.
/// </summary>
public class UserService : IUserService
{
    private readonly DataContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileService _fileService;

    public UserService(
        DataContext context,
        UserManager<User> userManager,
        ICurrentUserService currentUser,
        IFileService fileService)
    {
        _context = context;
        _userManager = userManager;
        _currentUser = currentUser;
        _fileService = fileService;
    }

    public async Task<PagedResponse<List<GetUserDto>>> GetUsersAsync(
        string? userName, string? email, int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        // Скрываем пользователей, с которыми есть блокировка в любую сторону.
        var blockRelatedIds = AccessGuard.BlockRelatedUserIds(_context, currentId);

        var query = _context.Users.AsNoTracking()
            .Where(u => !blockRelatedIds.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(userName))
        {
            var term = userName.Trim().ToLower();
            query = query.Where(u => u.UserName != null && u.UserName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var term = email.Trim().ToLower();
            query = query.Where(u => u.Email != null && u.Email.ToLower().Contains(term));
        }

        var total = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(u => new GetUserDto
            {
                Id = u.Id,
                UserName = u.UserName!,
                Email = u.Email!,
                FullName = u.FullName,
                Avatar = u.Avatar,
                IsVerified = u.IsVerified
            })
            .ToListAsync();

        return new PagedResponse<List<GetUserDto>>(users, total, page, size);
    }

    // ── История текстового поиска ─────────────────────────────────────────────

    public async Task<Response<GetSearchHistoryDto>> AddSearchHistoryAsync(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new BadRequestException("Текст поиска обязателен.");

        var userId = _currentUser.GetRequiredUserId();

        var entity = new SearchHistory
        {
            UserId = userId,
            Text = text.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.SearchHistories.Add(entity);
        await _context.SaveChangesAsync();

        return new Response<GetSearchHistoryDto>(new GetSearchHistoryDto
        {
            Id = entity.Id,
            Text = entity.Text,
            CreatedAt = entity.CreatedAt
        });
    }

    public async Task<Response<List<GetSearchHistoryDto>>> GetSearchHistoriesAsync()
    {
        var userId = _currentUser.GetRequiredUserId();

        var items = await _context.SearchHistories.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new GetSearchHistoryDto
            {
                Id = s.Id,
                Text = s.Text,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return new Response<List<GetSearchHistoryDto>>(items);
    }

    public async Task<Response<bool>> DeleteSearchHistoryAsync(int id)
    {
        var userId = _currentUser.GetRequiredUserId();

        var entity = await _context.SearchHistories
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId)
            ?? throw new NotFoundException("Запись истории поиска не найдена.");

        _context.SearchHistories.Remove(entity);
        await _context.SaveChangesAsync();

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> DeleteSearchHistoriesAsync()
    {
        var userId = _currentUser.GetRequiredUserId();

        var items = await _context.SearchHistories
            .Where(s => s.UserId == userId)
            .ToListAsync();

        _context.SearchHistories.RemoveRange(items);
        await _context.SaveChangesAsync();

        return new Response<bool>(true);
    }

    // ── История просмотренных профилей ────────────────────────────────────────

    public async Task<Response<GetUserSearchHistoryDto>> AddUserSearchHistoryAsync(string? userSearchId)
    {
        if (string.IsNullOrWhiteSpace(userSearchId))
            throw new BadRequestException("Id просматриваемого пользователя обязателен.");

        var userId = _currentUser.GetRequiredUserId();

        if (userSearchId == userId)
            throw new BadRequestException("Нельзя добавить в историю собственный профиль.");

        var searched = await _context.Users.FirstOrDefaultAsync(u => u.Id == userSearchId)
            ?? throw new NotFoundException("Просматриваемый пользователь не найден.");

        // История «просмотренных профилей» дедуплицируется: повторный просмотр
        // поднимает запись наверх (удаляем прежнюю, добавляем свежую).
        var existing = await _context.UserSearchHistories
            .Where(h => h.UserId == userId && h.SearchedUserId == userSearchId)
            .ToListAsync();
        if (existing.Count > 0)
            _context.UserSearchHistories.RemoveRange(existing);

        var entity = new UserSearchHistory
        {
            UserId = userId,
            SearchedUserId = searched.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserSearchHistories.Add(entity);
        await _context.SaveChangesAsync();

        return new Response<GetUserSearchHistoryDto>(new GetUserSearchHistoryDto
        {
            Id = entity.Id,
            SearchedUserId = searched.Id,
            SearchedUserName = searched.UserName!,
            SearchedUserAvatar = searched.Avatar,
            CreatedAt = entity.CreatedAt
        });
    }

    public async Task<Response<List<GetUserSearchHistoryDto>>> GetUserSearchHistoriesAsync()
    {
        var userId = _currentUser.GetRequiredUserId();

        var items = await _context.UserSearchHistories.AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new GetUserSearchHistoryDto
            {
                Id = h.Id,
                SearchedUserId = h.SearchedUserId,
                SearchedUserName = h.SearchedUser!.UserName!,
                SearchedUserAvatar = h.SearchedUser.Avatar,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();

        return new Response<List<GetUserSearchHistoryDto>>(items);
    }

    public async Task<Response<bool>> DeleteUserSearchHistoryAsync(int id)
    {
        var userId = _currentUser.GetRequiredUserId();

        var entity = await _context.UserSearchHistories
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId)
            ?? throw new NotFoundException("Запись истории просмотров не найдена.");

        _context.UserSearchHistories.Remove(entity);
        await _context.SaveChangesAsync();

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> DeleteUserSearchHistoriesAsync()
    {
        var userId = _currentUser.GetRequiredUserId();

        var items = await _context.UserSearchHistories
            .Where(h => h.UserId == userId)
            .ToListAsync();

        _context.UserSearchHistories.RemoveRange(items);
        await _context.SaveChangesAsync();

        return new Response<bool>(true);
    }

    // ── Удаление пользователя (только Admin) ──────────────────────────────────

    public async Task<Response<bool>> DeleteUserAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Id пользователя обязателен.");

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("Пользователь не найден.");

        // Собираем имена файлов ДО удаления (каскады уберут строки из БД, но не файлы с диска).
        var filesToDelete = new List<string?> { user.Avatar };

        filesToDelete.AddRange(await _context.UserProfiles
            .Where(p => p.UserId == userId && p.Image != null)
            .Select(p => p.Image)
            .ToListAsync());

        filesToDelete.AddRange(await _context.PostImages
            .Where(i => i.Post!.UserId == userId)
            .Select(i => i.ImageName)
            .ToListAsync());

        filesToDelete.AddRange(await _context.Stories
            .Where(s => s.UserId == userId && s.FileName != null)
            .Select(s => s.FileName)
            .ToListAsync());

        filesToDelete.AddRange(await _context.Messages
            .Where(m => m.SenderUserId == userId && m.FileName != null)
            .Select(m => m.FileName)
            .ToListAsync());

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

        foreach (var file in filesToDelete)
            _fileService.DeleteFile(file);

        return new Response<bool>(true);
    }
}
