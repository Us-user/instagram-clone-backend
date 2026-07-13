using Domain.DTOs.Post;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Responses;
using FluentValidation;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Посты и взаимодействия: CRUD, ленты, лайки/просмотры/комменты/избранное. Id текущего
/// юзера — из claims; удалять чужие посты/комменты нельзя. Проекция в DTO — общий
/// <see cref="PostProjections.ToDto"/> (счётчики + isLiked/isFavorite текущего юзера).
/// </summary>
public class PostService : IPostService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileService _fileService;
    private readonly IValidator<AddPostCommentDto> _commentValidator;
    private readonly IValidator<AddPostFavoriteDto> _favoriteValidator;

    public PostService(
        DataContext context,
        ICurrentUserService currentUser,
        IFileService fileService,
        IValidator<AddPostCommentDto> commentValidator,
        IValidator<AddPostFavoriteDto> favoriteValidator)
    {
        _context = context;
        _currentUser = currentUser;
        _fileService = fileService;
        _commentValidator = commentValidator;
        _favoriteValidator = favoriteValidator;
    }

    public async Task<PagedResponse<List<GetPostDto>>> GetPostsAsync(
        string? userId, string? title, string? content, int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        var query = _context.Posts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(p => p.UserId == userId);
        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(p => p.Title != null && p.Title.ToLower().Contains(title.ToLower()));
        if (!string.IsNullOrWhiteSpace(content))
            query = query.Where(p => p.Content != null && p.Content.ToLower().Contains(content.ToLower()));

        return await ToPagedAsync(query.OrderByDescending(p => p.CreatedAt), currentId, page, size);
    }

    public async Task<PagedResponse<List<GetPostDto>>> GetReelsAsync(int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        var query = _context.Posts.AsNoTracking()
            .Where(p => p.IsReel)
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedAsync(query, currentId, page, size);
    }

    public async Task<Response<GetPostDto>> GetByIdAsync(int? id)
    {
        if (id is null or <= 0)
            throw new BadRequestException("Некорректный Id поста.");

        var currentId = _currentUser.GetRequiredUserId();

        var post = await _context.Posts.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(PostProjections.ToDto(currentId))
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Пост не найден.");

        return new Response<GetPostDto>(post);
    }

    public async Task<Response<List<GetPostDto>>> GetMyPostsAsync()
    {
        var currentId = _currentUser.GetRequiredUserId();

        var posts = await _context.Posts.AsNoTracking()
            .Where(p => p.UserId == currentId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(PostProjections.ToDto(currentId))
            .ToListAsync();

        return new Response<List<GetPostDto>>(posts);
    }

    public async Task<PagedResponse<List<GetPostDto>>> GetFollowingPostAsync(
        string? userId, int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        // Чью ленту показываем: переданный userId или (по умолчанию) текущий юзер.
        var targetUserId = string.IsNullOrWhiteSpace(userId) ? currentId : userId;

        // Id тех, на кого подписан target — источник постов ленты.
        var followingIds = _context.FollowingRelationShips.AsNoTracking()
            .Where(f => f.UserId == targetUserId)
            .Select(f => f.FollowingUserId);

        var query = _context.Posts.AsNoTracking()
            .Where(p => followingIds.Contains(p.UserId))
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedAsync(query, currentId, page, size);
    }

    public async Task<Response<GetPostDto>> AddPostAsync(AddPostDto dto)
    {
        if (dto.Images is null || dto.Images.Count == 0)
            throw new BadRequestException("Необходимо прикрепить хотя бы одно изображение.");

        var currentId = _currentUser.GetRequiredUserId();

        // Сохраняем файлы до создания сущности; собранные имена кладём в PostImage.
        var imageNames = new List<string>();
        foreach (var file in dto.Images)
            imageNames.Add(await _fileService.SaveFileAsync(file));

        var post = new Post
        {
            UserId = currentId,
            Title = dto.Title,
            Content = dto.Content,
            IsReel = dto.IsReel,
            CreatedAt = DateTime.UtcNow,
            Images = imageNames.Select(name => new PostImage { ImageName = name }).ToList()
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        var result = await _context.Posts.AsNoTracking()
            .Where(p => p.Id == post.Id)
            .Select(PostProjections.ToDto(currentId))
            .FirstAsync();

        return new Response<GetPostDto>(result);
    }

    public async Task<Response<bool>> DeletePostAsync(int? id)
    {
        if (id is null or <= 0)
            throw new BadRequestException("Некорректный Id поста.");

        var currentId = _currentUser.GetRequiredUserId();

        var post = await _context.Posts
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Пост не найден.");

        if (post.UserId != currentId)
            throw new ForbiddenException("Нельзя удалить чужой пост.");

        // Имена файлов собираем до удаления; каскады EF уберут PostImage/Like/View/etc из БД.
        var imageNames = post.Images.Select(i => i.ImageName).ToList();

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        foreach (var name in imageNames)
            _fileService.DeleteFile(name);

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> LikePostAsync(int? postId)
    {
        if (postId is null or <= 0)
            throw new BadRequestException("Некорректный Id поста.");

        var currentId = _currentUser.GetRequiredUserId();
        await EnsurePostExistsAsync(postId.Value);

        var existing = await _context.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == currentId);

        bool liked;
        if (existing is not null)
        {
            _context.PostLikes.Remove(existing);
            liked = false;
        }
        else
        {
            _context.PostLikes.Add(new PostLike
            {
                PostId = postId.Value,
                UserId = currentId,
                CreatedAt = DateTime.UtcNow
            });
            liked = true;
        }

        await _context.SaveChangesAsync();
        return new Response<bool>(liked);
    }

    public async Task<Response<bool>> ViewPostAsync(int? postId)
    {
        if (postId is null or <= 0)
            throw new BadRequestException("Некорректный Id поста.");

        var currentId = _currentUser.GetRequiredUserId();
        await EnsurePostExistsAsync(postId.Value);

        // Просмотр уникален на юзера — повторный вызов идемпотентен.
        var alreadyViewed = await _context.PostViews
            .AnyAsync(v => v.PostId == postId && v.UserId == currentId);

        if (!alreadyViewed)
        {
            _context.PostViews.Add(new PostView
            {
                PostId = postId.Value,
                UserId = currentId
            });
            await _context.SaveChangesAsync();
        }

        return new Response<bool>(true);
    }

    public async Task<Response<GetPostCommentDto>> AddCommentAsync(AddPostCommentDto dto)
    {
        await _commentValidator.ValidateAndThrowAsync(dto);

        var currentId = _currentUser.GetRequiredUserId();
        await EnsurePostExistsAsync(dto.PostId);

        var comment = new PostComment
        {
            PostId = dto.PostId,
            UserId = currentId,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        _context.PostComments.Add(comment);
        await _context.SaveChangesAsync();

        var result = await _context.PostComments.AsNoTracking()
            .Where(c => c.Id == comment.Id)
            .Select(c => new GetPostCommentDto
            {
                Id = c.Id,
                PostId = c.PostId,
                Comment = c.Comment,
                CreatedAt = c.CreatedAt,
                UserId = c.UserId,
                UserName = c.User!.UserName!,
                UserImage = c.User.Avatar
            })
            .FirstAsync();

        return new Response<GetPostCommentDto>(result);
    }

    public async Task<Response<bool>> DeleteCommentAsync(int? commentId)
    {
        if (commentId is null or <= 0)
            throw new BadRequestException("Некорректный Id комментария.");

        var currentId = _currentUser.GetRequiredUserId();

        var comment = await _context.PostComments
            .FirstOrDefaultAsync(c => c.Id == commentId)
            ?? throw new NotFoundException("Комментарий не найден.");

        if (comment.UserId != currentId)
            throw new ForbiddenException("Нельзя удалить чужой комментарий.");

        _context.PostComments.Remove(comment);
        await _context.SaveChangesAsync();

        return new Response<bool>(true);
    }

    public async Task<Response<bool>> AddPostFavoriteAsync(AddPostFavoriteDto dto)
    {
        await _favoriteValidator.ValidateAndThrowAsync(dto);

        var currentId = _currentUser.GetRequiredUserId();
        await EnsurePostExistsAsync(dto.PostId);

        var existing = await _context.PostFavorites
            .FirstOrDefaultAsync(f => f.PostId == dto.PostId && f.UserId == currentId);

        bool favorite;
        if (existing is not null)
        {
            _context.PostFavorites.Remove(existing);
            favorite = false;
        }
        else
        {
            _context.PostFavorites.Add(new PostFavorite
            {
                PostId = dto.PostId,
                UserId = currentId,
                CreatedAt = DateTime.UtcNow
            });
            favorite = true;
        }

        await _context.SaveChangesAsync();
        return new Response<bool>(favorite);
    }

    /// <summary>Пагинированная материализация запроса постов через общую проекцию в DTO.</summary>
    private static async Task<PagedResponse<List<GetPostDto>>> ToPagedAsync(
        IQueryable<Post> query, string currentId, int page, int size)
    {
        var total = await query.CountAsync();

        var posts = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(PostProjections.ToDto(currentId))
            .ToListAsync();

        return new PagedResponse<List<GetPostDto>>(posts, total, page, size);
    }

    /// <summary>Гарантирует существование поста; иначе <see cref="NotFoundException"/> (404).</summary>
    private async Task EnsurePostExistsAsync(int postId)
    {
        var exists = await _context.Posts.AnyAsync(p => p.Id == postId);
        if (!exists)
            throw new NotFoundException("Пост не найден.");
    }
}
