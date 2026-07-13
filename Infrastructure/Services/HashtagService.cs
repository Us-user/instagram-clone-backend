using Domain.DTOs.Hashtag;
using Domain.DTOs.Post;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Responses;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Хэштеги (Phase 13, §3): разбор <c>#tag</c> из Title/Content поста (upsert + инкремент
/// <c>PostsCount</c>) и декремент при удалении, поиск по префиксу, лента постов по тегу и
/// тренды за период. Id текущего юзера — из claims (для фильтрации ленты по тегу).
/// </summary>
public class HashtagService : IHashtagService
{
    /// <summary>Окно «трендовости»: учитываем посты за последние 7 дней.</summary>
    private static readonly TimeSpan TrendingWindow = TimeSpan.FromDays(7);

    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;

    public HashtagService(DataContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task ProcessPostHashtagsAsync(int postId, string? title, string? content)
    {
        var tags = TextParsing.ExtractHashtags($"{title} {content}");
        if (tags.Count == 0)
            return;

        // Существующие теги тянем одним запросом (tracked), недостающие создаём по ходу.
        var known = await _context.Hashtags
            .Where(h => tags.Contains(h.Tag))
            .ToListAsync();

        foreach (var tag in tags)
        {
            var hashtag = known.FirstOrDefault(h => h.Tag == tag);
            if (hashtag is null)
            {
                hashtag = new Hashtag { Tag = tag, CreatedAt = DateTime.UtcNow };
                _context.Hashtags.Add(hashtag);
                known.Add(hashtag);
            }

            hashtag.PostsCount++;
            // Навигация Hashtag связывает FK и для новых (ещё без Id) тегов.
            _context.PostHashtags.Add(new PostHashtag { PostId = postId, Hashtag = hashtag });
        }

        await _context.SaveChangesAsync();
    }

    public async Task RemovePostHashtagsAsync(int postId)
    {
        var links = await _context.PostHashtags
            .Include(ph => ph.Hashtag)
            .Where(ph => ph.PostId == postId)
            .ToListAsync();

        if (links.Count == 0)
            return;

        foreach (var link in links)
        {
            if (link.Hashtag is not null && link.Hashtag.PostsCount > 0)
                link.Hashtag.PostsCount--;
        }

        _context.PostHashtags.RemoveRange(links);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResponse<List<GetHashtagDto>>> SearchAsync(
        string? query, int? pageNumber, int? pageSize)
    {
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        var hashtags = _context.Hashtags.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var prefix = query.Trim().TrimStart('#').ToLower();
            hashtags = hashtags.Where(h => h.Tag.StartsWith(prefix));
        }

        var ordered = hashtags
            .OrderByDescending(h => h.PostsCount)
            .ThenBy(h => h.Tag);

        var total = await ordered.CountAsync();
        var items = await ordered
            .Skip((page - 1) * size)
            .Take(size)
            .Select(ToDto)
            .ToListAsync();

        return new PagedResponse<List<GetHashtagDto>>(items, total, page, size);
    }

    public async Task<PagedResponse<List<GetPostDto>>> GetPostsByTagAsync(
        string? tag, int? pageNumber, int? pageSize)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new BadRequestException("Тег обязателен.");

        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);
        var normalized = tag.Trim().TrimStart('#').ToLower();

        // Свежие сверху; блокировки/приватность — через общий фильтр лент.
        var query = _context.Posts.AsNoTracking()
            .Where(p => p.PostHashtags.Any(ph => ph.Hashtag!.Tag == normalized))
            .VisibleTo(_context, currentId)
            .OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync();
        var posts = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(PostProjections.ToDto(currentId))
            .ToListAsync();

        await MentionEnrichment.EnrichPostsAsync(_context, posts);

        return new PagedResponse<List<GetPostDto>>(posts, total, page, size);
    }

    public async Task<PagedResponse<List<GetHashtagDto>>> GetTrendingAsync(
        int? pageNumber, int? pageSize)
    {
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);
        var since = DateTime.UtcNow - TrendingWindow;

        // Трендовость = число постов с тегом за окно; при равенстве — по общей популярности.
        var ordered = _context.Hashtags.AsNoTracking()
            .Select(h => new
            {
                Hashtag = h,
                RecentCount = h.PostHashtags.Count(ph => ph.Post!.CreatedAt >= since)
            })
            .Where(x => x.RecentCount > 0)
            .OrderByDescending(x => x.RecentCount)
            .ThenByDescending(x => x.Hashtag.PostsCount)
            .Select(x => x.Hashtag);

        var total = await ordered.CountAsync();
        var items = await ordered
            .Skip((page - 1) * size)
            .Take(size)
            .Select(ToDto)
            .ToListAsync();

        return new PagedResponse<List<GetHashtagDto>>(items, total, page, size);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Hashtag, GetHashtagDto>> ToDto =
        h => new GetHashtagDto
        {
            Id = h.Id,
            Tag = h.Tag,
            PostsCount = h.PostsCount,
            CreatedAt = h.CreatedAt
        };
}
