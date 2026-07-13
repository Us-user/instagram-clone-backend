using Domain.DTOs.Story;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Responses;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Сторис: ленты (подписки/юзер/мои), лайк-тумблер, уникальный просмотр, создание из поста
/// или файла, удаление автором. Сторис живёт 24 часа — во всех выборках применяется окно
/// <c>CreatedAt &gt; UtcNow - 24ч</c>. Id текущего юзера берётся из claims; чужую сторис удалять нельзя.
/// Проекция в DTO — общий <see cref="StoryProjections.ToDto"/>.
/// </summary>
public class StoryService : IStoryService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileService _fileService;

    public StoryService(DataContext context, ICurrentUserService currentUser, IFileService fileService)
    {
        _context = context;
        _currentUser = currentUser;
        _fileService = fileService;
    }

    /// <summary>Граница активности сторис: моложе 24 часов.</summary>
    private static DateTime ActiveSince => DateTime.UtcNow.AddHours(-24);

    public async Task<Response<List<GetStoryDto>>> GetStoriesAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();
        var since = ActiveSince;

        // Источник сторис ленты — авторы, на кого подписан текущий юзер.
        var followingIds = _context.FollowingRelationShips.AsNoTracking()
            .Where(f => f.UserId == currentId)
            .Select(f => f.FollowingUserId);

        // Группировка по авторам выражена сортировкой: сначала по автору, внутри — свежие выше.
        var stories = await _context.Stories.AsNoTracking()
            .Where(s => followingIds.Contains(s.UserId) && s.CreatedAt > since)
            .OrderBy(s => s.UserId)
            .ThenByDescending(s => s.CreatedAt)
            .Select(StoryProjections.ToDto())
            .ToListAsync();

        return new Response<List<GetStoryDto>>(stories);
    }

    public async Task<Response<List<GetStoryDto>>> GetUserStoriesAsync(string userId)
    {
        _currentUser.GetRequiredUserId();

        if (string.IsNullOrWhiteSpace(userId))
            throw new BadRequestException("Некорректный Id пользователя.");

        var since = ActiveSince;
        var stories = await _context.Stories.AsNoTracking()
            .Where(s => s.UserId == userId && s.CreatedAt > since)
            .OrderByDescending(s => s.CreatedAt)
            .Select(StoryProjections.ToDto())
            .ToListAsync();

        return new Response<List<GetStoryDto>>(stories);
    }

    public async Task<Response<List<GetStoryDto>>> GetMyStoriesAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();

        var since = ActiveSince;
        var stories = await _context.Stories.AsNoTracking()
            .Where(s => s.UserId == currentId && s.CreatedAt > since)
            .OrderByDescending(s => s.CreatedAt)
            .Select(StoryProjections.ToDto())
            .ToListAsync();

        return new Response<List<GetStoryDto>>(stories);
    }

    public async Task<Response<string>> LikeStoryAsync(int? storyId)
    {
        if (storyId is null or <= 0)
            throw new BadRequestException("Некорректный Id сторис.");

        var currentId = _currentUser.GetRequiredUserId();
        await EnsureStoryExistsAsync(storyId.Value);

        var existing = await _context.StoryLikes
            .FirstOrDefaultAsync(l => l.StoryId == storyId && l.UserId == currentId);

        string message;
        if (existing is not null)
        {
            _context.StoryLikes.Remove(existing);
            message = "Лайк убран.";
        }
        else
        {
            _context.StoryLikes.Add(new StoryLike { StoryId = storyId.Value, UserId = currentId });
            message = "Лайк добавлен.";
        }

        await _context.SaveChangesAsync();
        return new Response<string>(message);
    }

    public async Task<Response<GetStoryDto>> GetStoryByIdAsync(int? id)
    {
        if (id is null or <= 0)
            throw new BadRequestException("Некорректный Id сторис.");

        _currentUser.GetRequiredUserId();

        var story = await _context.Stories.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(StoryProjections.ToDto())
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Сторис не найдена.");

        return new Response<GetStoryDto>(story);
    }

    public async Task<Response<GetStoryDto>> AddStoriesAsync(int? postId, AddStoryDto dto)
    {
        var currentId = _currentUser.GetRequiredUserId();

        string? fileName = null;
        int? sourcePostId = null;

        if (postId is > 0)
        {
            // Сторис из поста: сохраняем ссылку на пост, собственного файла нет.
            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
            if (!postExists)
                throw new NotFoundException("Пост-источник не найден.");
            sourcePostId = postId;
        }
        else if (dto.Image is not null)
        {
            // Сторис из файла: сохраняем файл через общий IFileService.
            fileName = await _fileService.SaveFileAsync(dto.Image);
        }
        else
        {
            throw new BadRequestException("Нужно указать PostId или прикрепить файл сторис.");
        }

        var story = new Story
        {
            UserId = currentId,
            FileName = fileName,
            PostId = sourcePostId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Stories.Add(story);
        await _context.SaveChangesAsync();

        var result = await _context.Stories.AsNoTracking()
            .Where(s => s.Id == story.Id)
            .Select(StoryProjections.ToDto())
            .FirstAsync();

        return new Response<GetStoryDto>(result);
    }

    public async Task<Response<bool>> DeleteStoryAsync(int? id)
    {
        if (id is null or <= 0)
            throw new BadRequestException("Некорректный Id сторис.");

        var currentId = _currentUser.GetRequiredUserId();

        var story = await _context.Stories
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException("Сторис не найдена.");

        if (story.UserId != currentId)
            throw new ForbiddenException("Нельзя удалить чужую сторис.");

        // Собственный файл сторис удаляем с диска; сторис из поста своего файла не имеет.
        var fileName = story.FileName;

        _context.Stories.Remove(story);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(fileName))
            _fileService.DeleteFile(fileName);

        return new Response<bool>(true);
    }

    public async Task<Response<GetStoryViewDto>> AddStoryViewAsync(int? storyId)
    {
        if (storyId is null or <= 0)
            throw new BadRequestException("Некорректный Id сторис.");

        var currentId = _currentUser.GetRequiredUserId();
        await EnsureStoryExistsAsync(storyId.Value);

        // Просмотр уникален на юзера — повторный вызов возвращает существующую запись.
        var view = await _context.StoryViews
            .FirstOrDefaultAsync(v => v.StoryId == storyId && v.ViewUserId == currentId);

        if (view is null)
        {
            view = new StoryView { StoryId = storyId.Value, ViewUserId = currentId };
            _context.StoryViews.Add(view);
            await _context.SaveChangesAsync();
        }

        return new Response<GetStoryViewDto>(new GetStoryViewDto
        {
            Id = view.Id,
            ViewUserId = view.ViewUserId,
            StoryId = view.StoryId
        });
    }

    /// <summary>Гарантирует существование сторис; иначе <see cref="NotFoundException"/> (404).</summary>
    private async Task EnsureStoryExistsAsync(int storyId)
    {
        var exists = await _context.Stories.AnyAsync(s => s.Id == storyId);
        if (!exists)
            throw new NotFoundException("Сторис не найдена.");
    }
}
