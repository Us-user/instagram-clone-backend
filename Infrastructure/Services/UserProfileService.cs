using Domain.DTOs.Post;
using Domain.DTOs.UserProfile;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Responses;
using FluentValidation;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Профиль пользователя: чтение со счётчиками и флагом подписки, редактирование,
/// изображение профиля и избранные посты. Id текущего юзера — из claims.
/// </summary>
public class UserProfileService : IUserProfileService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileService _fileService;
    private readonly IValidator<UpdateUserProfileDto> _updateValidator;
    private readonly IImageUrlBuilder _imageUrls;

    public UserProfileService(
        DataContext context,
        ICurrentUserService currentUser,
        IFileService fileService,
        IValidator<UpdateUserProfileDto> updateValidator,
        IImageUrlBuilder imageUrls)
    {
        _context = context;
        _currentUser = currentUser;
        _fileService = fileService;
        _updateValidator = updateValidator;
        _imageUrls = imageUrls;
    }

    public async Task<Response<GetUserProfileDto>> GetByIdAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new BadRequestException("Id пользователя обязателен.");

        var currentId = _currentUser.GetRequiredUserId();

        // Заблокированному (в любую сторону) профиль не показываем — скрываем его существование.
        if (id != currentId && await AccessGuard.IsBlockBetweenAsync(_context, id, currentId))
            throw new NotFoundException("Профиль не найден.");

        var dto = await BuildProfileDtoAsync(id, currentId);
        return new Response<GetUserProfileDto>(dto);
    }

    public async Task<Response<bool>> IsFollowAsync(string? followingUserId)
    {
        if (string.IsNullOrWhiteSpace(followingUserId))
            throw new BadRequestException("Id пользователя обязателен.");

        var currentId = _currentUser.GetRequiredUserId();

        // «Подписан» = одобренная связь (Pending-запрос ещё не даёт подписки).
        var isFollowing = await _context.FollowingRelationShips
            .AnyAsync(f => f.UserId == currentId
                && f.FollowingUserId == followingUserId
                && f.Status == FollowStatus.Accepted);

        return new Response<bool>(isFollowing);
    }

    public async Task<Response<GetUserProfileDto>> GetMyProfileAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();
        var dto = await BuildProfileDtoAsync(currentId, currentId);
        return new Response<GetUserProfileDto>(dto);
    }

    public async Task<Response<GetUserProfileDto>> UpdateAsync(UpdateUserProfileDto dto)
    {
        await _updateValidator.ValidateAndThrowAsync(dto);

        var currentId = _currentUser.GetRequiredUserId();

        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == currentId)
            ?? throw new NotFoundException("Профиль не найден.");

        profile.About = dto.About;
        profile.Gender = dto.Gender;
        await _context.SaveChangesAsync();

        var result = await BuildProfileDtoAsync(currentId, currentId);
        return new Response<GetUserProfileDto>(result);
    }

    public async Task<PagedResponse<List<GetPostDto>>> GetPostFavoritesAsync(int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        var query = _context.PostFavorites.AsNoTracking()
            .Where(f => f.UserId == currentId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.Post!);

        var total = await query.CountAsync();

        // Список избранного текущего юзера — общая проекция даёт isFavorite = true для этих постов.
        var posts = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(PostProjections.ToDto(currentId))
            .ToListAsync();

        await MentionEnrichment.EnrichPostsAsync(_context, posts);
        ImageUrlEnrichment.FillPosts(_imageUrls, posts);

        return new PagedResponse<List<GetPostDto>>(posts, total, page, size);
    }

    public async Task<Response<string>> UpdateImageAsync(IFormFile? imageFile)
    {
        if (imageFile is null || imageFile.Length == 0)
            throw new BadRequestException("Файл изображения не передан.");

        var currentId = _currentUser.GetRequiredUserId();

        var profile = await _context.UserProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == currentId)
            ?? throw new NotFoundException("Профиль не найден.");

        var newFileName = await _fileService.SaveFileAsync(imageFile);

        // Старое изображение удаляем только после успешного сохранения нового.
        _fileService.DeleteFile(profile.Image);

        profile.Image = newFileName;
        // Денормализованный User.Avatar — источник аватара для лент, комментов, чатов, поиска и
        // уведомлений (эти проекции читают именно его). Держим его в синхроне с Image, иначе
        // загруженное фото видно только на самом профиле, а в остальных местах остаётся пустым.
        if (profile.User is not null)
            profile.User.Avatar = newFileName;
        await _context.SaveChangesAsync();

        return new Response<string>(newFileName);
    }

    public async Task<Response<bool>> DeleteImageAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();

        var profile = await _context.UserProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == currentId)
            ?? throw new NotFoundException("Профиль не найден.");

        if (string.IsNullOrWhiteSpace(profile.Image))
            throw new BadRequestException("Изображение профиля отсутствует.");

        _fileService.DeleteFile(profile.Image);
        profile.Image = null;
        // Держим денормализованный User.Avatar в синхроне (его читают ленты/комменты/чаты/поиск).
        if (profile.User is not null)
            profile.User.Avatar = null;
        await _context.SaveChangesAsync();

        return new Response<bool>(true);
    }

    /// <summary>
    /// Собирает <see cref="GetUserProfileDto"/> для <paramref name="userId"/>: данные профиля,
    /// счётчики постов/подписчиков/подписок и подписан ли <paramref name="currentId"/>.
    /// </summary>
    private async Task<GetUserProfileDto> BuildProfileDtoAsync(string userId, string currentId)
    {
        var profile = await _context.UserProfiles.AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile is null || profile.User is null)
            throw new NotFoundException("Профиль не найден.");

        return new GetUserProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            UserName = profile.User.UserName!,
            FullName = profile.User.FullName,
            About = profile.About,
            Gender = profile.Gender,
            Image = profile.Image,
            ImageUrl = _imageUrls.Build(profile.Image),
            IsPrivate = profile.User.IsPrivate,
            IsVerified = profile.User.IsVerified,
            // Счётчики видны и на приватном чужом профиле (сам контент — через свои эндпоинты).
            PostCount = await _context.Posts.CountAsync(p => p.UserId == userId),
            FollowersCount = await _context.FollowingRelationShips
                .CountAsync(f => f.FollowingUserId == userId && f.Status == FollowStatus.Accepted),
            FollowingCount = await _context.FollowingRelationShips
                .CountAsync(f => f.UserId == userId && f.Status == FollowStatus.Accepted),
            // На себя подписаться нельзя — для собственного профиля всегда false.
            IsFollowing = userId != currentId
                && await _context.FollowingRelationShips
                    .AnyAsync(f => f.UserId == currentId && f.FollowingUserId == userId
                        && f.Status == FollowStatus.Accepted),
            // Отправлен ли текущим пользователем ещё не одобренный запрос на подписку.
            IsRequested = userId != currentId
                && await _context.FollowingRelationShips
                    .AnyAsync(f => f.UserId == currentId && f.FollowingUserId == userId
                        && f.Status == FollowStatus.Pending)
        };
    }
}
