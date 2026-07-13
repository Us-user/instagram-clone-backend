using Domain.DTOs.Post;
using Domain.Entities;
using Domain.Enums;
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
    private readonly INotificationService _notifications;
    private readonly IHashtagService _hashtags;
    private readonly IMentionService _mentions;
    private readonly IValidator<AddPostCommentDto> _commentValidator;
    private readonly IValidator<AddPostFavoriteDto> _favoriteValidator;

    public PostService(
        DataContext context,
        ICurrentUserService currentUser,
        IFileService fileService,
        INotificationService notifications,
        IHashtagService hashtags,
        IMentionService mentions,
        IValidator<AddPostCommentDto> commentValidator,
        IValidator<AddPostFavoriteDto> favoriteValidator)
    {
        _context = context;
        _currentUser = currentUser;
        _fileService = fileService;
        _notifications = notifications;
        _hashtags = hashtags;
        _mentions = mentions;
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

        // Скрываем посты заблокированных (в обе стороны) и приватных без одобренной подписки.
        query = query.VisibleTo(_context, currentId);

        return await ToPagedAsync(query.OrderByDescending(p => p.CreatedAt), currentId, page, size);
    }

    public async Task<PagedResponse<List<GetPostDto>>> GetReelsAsync(int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        var query = _context.Posts.AsNoTracking()
            .Where(p => p.IsReel)
            .VisibleTo(_context, currentId)
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedAsync(query, currentId, page, size);
    }

    public async Task<Response<GetPostDto>> GetByIdAsync(int? id)
    {
        if (id is null or <= 0)
            throw new BadRequestException("Некорректный Id поста.");

        var currentId = _currentUser.GetRequiredUserId();

        // Скрытый по блокировке/приватности пост недоступен так же, как несуществующий.
        var post = await _context.Posts.AsNoTracking()
            .Where(p => p.Id == id)
            .VisibleTo(_context, currentId)
            .Select(PostProjections.ToDto(currentId))
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Пост не найден.");

        await MentionEnrichment.EnrichPostsAsync(_context, new List<GetPostDto> { post });

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

        await MentionEnrichment.EnrichPostsAsync(_context, posts);

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
            .VisibleTo(_context, currentId)
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

        // Хэштеги и упоминания разбираем после сохранения — нужен сгенерированный Id поста.
        await _hashtags.ProcessPostHashtagsAsync(post.Id, post.Title, post.Content);
        await _mentions.ProcessMentionsAsync(
            $"{post.Title} {post.Content}", currentId, MentionEntityType.Post, post.Id);

        var result = await _context.Posts.AsNoTracking()
            .Where(p => p.Id == post.Id)
            .Select(PostProjections.ToDto(currentId))
            .FirstAsync();

        await MentionEnrichment.EnrichPostsAsync(_context, new List<GetPostDto> { result });

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

        // Снимаем связи с хэштегами и декрементим их счётчики до удаления поста.
        await _hashtags.RemovePostHashtagsAsync(post.Id);

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
        var ownerId = await GetPostOwnerAsync(postId.Value);

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

        // Уведомление автору поста только при постановке лайка (не при снятии). «Не себе»
        // отсекается внутри NotificationService.
        if (liked)
            await _notifications.CreateAsync(
                ownerId, currentId, NotificationType.Like, NotificationEntityType.Post, postId.Value);

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

        // Пост и родитель ответа: для ответа пост берём у родителя (источник истины).
        var postId = dto.PostId;
        int? parentCommentId = null;
        string? replyToUserId = null;
        string? replyToUserName = null;

        if (dto.ParentCommentId is > 0)
        {
            var parent = await _context.PostComments.AsNoTracking()
                .Where(c => c.Id == dto.ParentCommentId)
                .Select(c => new { c.Id, c.PostId, c.UserId, c.ParentCommentId, UserName = c.User!.UserName! })
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException("Родительский комментарий не найден.");

            // Максимум 2 уровня: ответ на ответ крепится к родителю верхнего уровня.
            parentCommentId = parent.ParentCommentId ?? parent.Id;
            postId = parent.PostId;
            replyToUserId = parent.UserId;
            replyToUserName = parent.UserName;
        }

        var ownerId = await GetPostOwnerAsync(postId);

        // При ответе — автоподстановка @username того, кому отвечаешь, в начало текста.
        var text = dto.Comment;
        if (replyToUserName is not null
            && !text.TrimStart().StartsWith($"@{replyToUserName}", StringComparison.OrdinalIgnoreCase))
        {
            text = $"@{replyToUserName} {text}";
        }

        var comment = new PostComment
        {
            PostId = postId,
            ParentCommentId = parentCommentId,
            UserId = currentId,
            Comment = text,
            CreatedAt = DateTime.UtcNow
        };

        _context.PostComments.Add(comment);
        await _context.SaveChangesAsync();

        // Ответ → CommentReply автору исходного коммента; иначе Comment автору поста.
        // «Не себе» отсекается внутри NotificationService.
        if (replyToUserId is not null)
            await _notifications.CreateAsync(
                replyToUserId, currentId, NotificationType.CommentReply, NotificationEntityType.Comment, comment.Id);
        else
            await _notifications.CreateAsync(
                ownerId, currentId, NotificationType.Comment, NotificationEntityType.Post, postId);

        // Упоминания (@username) в тексте → Mention + уведомление Mention. Для адресата ответа
        // запись Mention создаётся (кликабельная ссылка), но его Mention-уведомление подавляем —
        // он уже получил CommentReply.
        await _mentions.ProcessMentionsAsync(
            comment.Comment, currentId, MentionEntityType.Comment, comment.Id,
            suppressNotificationUserId: replyToUserId);

        var result = await _context.PostComments.AsNoTracking()
            .Where(c => c.Id == comment.Id)
            .Select(CommentProjections.ToDto(currentId))
            .FirstAsync();

        await MentionEnrichment.EnrichCommentsAsync(_context, new List<GetPostCommentDto> { result });

        return new Response<GetPostCommentDto>(result);
    }

    public async Task<Response<bool>> LikeCommentAsync(int? commentId)
    {
        if (commentId is null or <= 0)
            throw new BadRequestException("Некорректный Id комментария.");

        var currentId = _currentUser.GetRequiredUserId();

        var comment = await _context.PostComments.AsNoTracking()
            .Where(c => c.Id == commentId)
            .Select(c => new { c.Id, c.UserId })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Комментарий не найден.");

        var existing = await _context.CommentLikes
            .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == currentId);

        bool liked;
        if (existing is not null)
        {
            _context.CommentLikes.Remove(existing);
            liked = false;
        }
        else
        {
            _context.CommentLikes.Add(new CommentLike
            {
                CommentId = commentId.Value,
                UserId = currentId,
                CreatedAt = DateTime.UtcNow
            });
            liked = true;
        }

        await _context.SaveChangesAsync();

        // Уведомление автору коммента только при постановке лайка («не себе» отсекается в сервисе).
        if (liked)
            await _notifications.CreateAsync(
                comment.UserId, currentId, NotificationType.CommentLike, NotificationEntityType.Comment, comment.Id);

        return new Response<bool>(liked);
    }

    public async Task<PagedResponse<List<GetPostCommentDto>>> GetCommentRepliesAsync(
        int? commentId, int? pageNumber, int? pageSize)
    {
        if (commentId is null or <= 0)
            throw new BadRequestException("Некорректный Id комментария.");

        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        var exists = await _context.PostComments.AnyAsync(c => c.Id == commentId);
        if (!exists)
            throw new NotFoundException("Комментарий не найден.");

        // Скрываем ответы авторов, с которыми есть блокировка в любую сторону.
        var blockedIds = AccessGuard.BlockRelatedUserIds(_context, currentId);

        var query = _context.PostComments.AsNoTracking()
            .Where(c => c.ParentCommentId == commentId && !blockedIds.Contains(c.UserId))
            .OrderBy(c => c.CreatedAt);

        var total = await query.CountAsync();

        var replies = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(CommentProjections.ToDto(currentId))
            .ToListAsync();

        await MentionEnrichment.EnrichCommentsAsync(_context, replies);

        return new PagedResponse<List<GetPostCommentDto>>(replies, total, page, size);
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
    private async Task<PagedResponse<List<GetPostDto>>> ToPagedAsync(
        IQueryable<Post> query, string currentId, int page, int size)
    {
        var total = await query.CountAsync();

        var posts = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(PostProjections.ToDto(currentId))
            .ToListAsync();

        await MentionEnrichment.EnrichPostsAsync(_context, posts);

        return new PagedResponse<List<GetPostDto>>(posts, total, page, size);
    }

    /// <summary>Гарантирует существование поста; иначе <see cref="NotFoundException"/> (404).</summary>
    private async Task EnsurePostExistsAsync(int postId)
    {
        var exists = await _context.Posts.AnyAsync(p => p.Id == postId);
        if (!exists)
            throw new NotFoundException("Пост не найден.");
    }

    /// <summary>
    /// Возвращает Id автора поста (для адресации уведомления); иначе
    /// <see cref="NotFoundException"/> (404) — заодно проверяет существование поста.
    /// </summary>
    private async Task<string> GetPostOwnerAsync(int postId)
    {
        var ownerId = await _context.Posts.AsNoTracking()
            .Where(p => p.Id == postId)
            .Select(p => p.UserId)
            .FirstOrDefaultAsync();

        return ownerId ?? throw new NotFoundException("Пост не найден.");
    }
}
