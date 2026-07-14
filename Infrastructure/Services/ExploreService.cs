using Domain.DTOs.Post;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Explore / рекомендации (§12): content-based рекомендательный движок «как в инсте», без ML.
/// Профиль интересов считается на лету (без материализованного <c>UserInterest</c>): по
/// взаимодействиям пользователя собираются веса любимых хэштегов и авторов с затуханием по
/// времени, затем кандидаты скорятся и перемешиваются с учётом разнообразия. Всё персональное
/// ранжирование делается in-memory над ограниченным пулом кандидатов; проекция в DTO —
/// общий <see cref="PostProjections.ToDto"/>, как в остальных лентах.
/// </summary>
public class ExploreService : IExploreService
{
    private readonly DataContext _context;
    private readonly ICurrentUserService _currentUser;

    // Веса типов взаимодействий для профиля интересов: favorite > comment > like > view (§12).
    private const double FavoriteWeight = 4.0;
    private const double CommentWeight = 3.0;
    private const double LikeWeight = 2.0;
    private const double ViewWeight = 1.0;

    // Затухание интересов по времени: чем старше действие, тем меньше вес (период полураспада).
    private const double InterestHalfLifeDays = 30.0;

    // Свежесть кандидата: пост «полусвежий» через столько дней.
    private const double FreshnessHalfLifeDays = 7.0;

    // Веса компонент скоринга кандидата (в сумме 1): хэштеги > автор > популярность ≈ свежесть.
    private const double HashtagScoreWeight = 0.40;
    private const double AuthorScoreWeight = 0.30;
    private const double PopularityScoreWeight = 0.15;
    private const double FreshnessScoreWeight = 0.15;

    // Разнообразие: не более N постов подряд от одного автора.
    private const int MaxConsecutiveSameAuthor = 2;

    // Ограничение пула кандидатов (свежайшие сверху), чтобы in-memory скоринг не тянул всю таблицу.
    private const int MaxCandidates = 500;

    public ExploreService(DataContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<List<GetPostDto>>> GetFeedAsync(int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        // 1. Профиль интересов: веса хэштегов и авторов из взаимодействий (с затуханием по времени).
        var (hashtagInterest, authorInterest) = await BuildInterestProfileAsync(currentId);

        // 2. Кандидаты: доступные посты чужих авторов, на которых не подписан и которые ещё не
        //    просмотрел. VisibleTo отсекает блок (в обе стороны) и приватные без Accepted-подписки;
        //    затем убираем свои посты и авторов, на которых уже подписан (Explore — про новое).
        var followingIds = _context.FollowingRelationShips.AsNoTracking()
            .Where(f => f.UserId == currentId && f.Status == FollowStatus.Accepted)
            .Select(f => f.FollowingUserId);

        var scalar = await _context.Posts.AsNoTracking()
            .VisibleTo(_context, currentId)
            .Where(p => p.UserId != currentId)
            .Where(p => !followingIds.Contains(p.UserId))
            .Where(p => !p.Views.Any(v => v.UserId == currentId))
            .OrderByDescending(p => p.CreatedAt)
            .Take(MaxCandidates)
            .Select(p => new
            {
                p.Id,
                p.UserId,
                p.CreatedAt,
                Popularity = p.Likes.Count + p.Comments.Count + p.Views.Count
            })
            .ToListAsync();

        if (scalar.Count == 0)
            return new PagedResponse<List<GetPostDto>>(new List<GetPostDto>(), 0, page, size);

        // Хэштеги кандидатов — отдельным батч-запросом (без коллекционной проекции в SQL).
        var hashtagsByPost = await LoadHashtagIdsByPostAsync(scalar.Select(s => s.Id).ToList());

        var candidates = scalar
            .Select(s => new CandidateRow
            {
                Id = s.Id,
                UserId = s.UserId,
                CreatedAt = s.CreatedAt,
                Popularity = s.Popularity,
                HashtagIds = hashtagsByPost.TryGetValue(s.Id, out var h) ? h : new List<int>()
            })
            .ToList();

        // 3. Скоринг + разнообразие → упорядоченный список Id постов.
        var orderedIds = ScoreAndDiversify(candidates, hashtagInterest, authorInterest);

        // 4. Материализуем текущую страницу, сохраняя порядок ранжирования.
        return await MaterializePageAsync(orderedIds, currentId, page, size);
    }

    public async Task<PagedResponse<List<GetPostDto>>> GetPopularAsync(int? pageNumber, int? pageSize)
    {
        var currentId = _currentUser.GetRequiredUserId();
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        // Cold start: чистая популярность (лайки+комменты+просмотры), свежесть — тай-брейк.
        // Фильтры доступа сохраняем (VisibleTo: блок/приват), исключаем свои посты — Explore про чужое.
        var query = _context.Posts.AsNoTracking()
            .VisibleTo(_context, currentId)
            .Where(p => p.UserId != currentId)
            .OrderByDescending(p => p.Likes.Count + p.Comments.Count + p.Views.Count)
            .ThenByDescending(p => p.CreatedAt);

        var total = await query.CountAsync();

        var posts = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(PostProjections.ToDto(currentId))
            .ToListAsync();

        await MentionEnrichment.EnrichPostsAsync(_context, posts);

        return new PagedResponse<List<GetPostDto>>(posts, total, page, size);
    }

    /// <summary>
    /// Строит профиль интересов текущего юзера: словари весов по хэштегам и авторам. Сначала
    /// каждому посту, с которым он взаимодействовал, назначается вес (тип действия × затухание по
    /// времени; несколько действий по посту суммируются), затем этот вес разворачивается в веса
    /// хэштегов поста и его автора. Пустые словари → «холодный» юзер (лента деградирует к
    /// популярности+свежести — компоненты хэштегов/автора обнулятся при скоринге).
    /// </summary>
    private async Task<(Dictionary<int, double> Hashtags, Dictionary<string, double> Authors)>
        BuildInterestProfileAsync(string currentId)
    {
        var now = DateTime.UtcNow;
        var postWeights = new Dictionary<int, double>();

        void Accumulate(int postId, double typeWeight, DateTime? at)
        {
            var weight = typeWeight * (at.HasValue ? TimeDecay(now, at.Value, InterestHalfLifeDays) : 1.0);
            postWeights[postId] = postWeights.TryGetValue(postId, out var cur) ? cur + weight : weight;
        }

        var favorites = await _context.PostFavorites.AsNoTracking()
            .Where(f => f.UserId == currentId)
            .Select(f => new { f.PostId, f.CreatedAt })
            .ToListAsync();
        foreach (var f in favorites)
            Accumulate(f.PostId, FavoriteWeight, f.CreatedAt);

        var comments = await _context.PostComments.AsNoTracking()
            .Where(c => c.UserId == currentId)
            .Select(c => new { c.PostId, c.CreatedAt })
            .ToListAsync();
        foreach (var c in comments)
            Accumulate(c.PostId, CommentWeight, c.CreatedAt);

        var likes = await _context.PostLikes.AsNoTracking()
            .Where(l => l.UserId == currentId)
            .Select(l => new { l.PostId, l.CreatedAt })
            .ToListAsync();
        foreach (var l in likes)
            Accumulate(l.PostId, LikeWeight, l.CreatedAt);

        // Просмотры без отметки времени — вклад без затухания (вес просмотра и так наименьший).
        var viewedPostIds = await _context.PostViews.AsNoTracking()
            .Where(v => v.UserId == currentId)
            .Select(v => v.PostId)
            .ToListAsync();
        foreach (var pid in viewedPostIds)
            Accumulate(pid, ViewWeight, null);

        var hashtagInterest = new Dictionary<int, double>();
        var authorInterest = new Dictionary<string, double>();

        if (postWeights.Count == 0)
            return (hashtagInterest, authorInterest);

        var interactedIds = postWeights.Keys.ToList();

        var authorsByPost = await _context.Posts.AsNoTracking()
            .Where(p => interactedIds.Contains(p.Id))
            .Select(p => new { p.Id, p.UserId })
            .ToListAsync();

        var hashtagsByPost = await LoadHashtagIdsByPostAsync(interactedIds);

        foreach (var post in authorsByPost)
        {
            var weight = postWeights[post.Id];

            authorInterest[post.UserId] =
                authorInterest.TryGetValue(post.UserId, out var a) ? a + weight : weight;

            if (hashtagsByPost.TryGetValue(post.Id, out var hids))
                foreach (var hid in hids)
                    hashtagInterest[hid] =
                        hashtagInterest.TryGetValue(hid, out var h) ? h + weight : weight;
        }

        return (hashtagInterest, authorInterest);
    }

    /// <summary>
    /// Считает балл каждого кандидата (нормируя компоненты хэштегов/автора/популярности в [0..1] по
    /// максимуму пула; свежесть уже в [0..1]), сортирует по убыванию балла и применяет разнообразие
    /// (не более N подряд от одного автора). Возвращает упорядоченные Id постов.
    /// </summary>
    private static List<int> ScoreAndDiversify(
        List<CandidateRow> candidates,
        Dictionary<int, double> hashtagInterest,
        Dictionary<string, double> authorInterest)
    {
        var now = DateTime.UtcNow;

        foreach (var c in candidates)
        {
            c.RawHashtag = c.HashtagIds.Sum(hid => hashtagInterest.TryGetValue(hid, out var w) ? w : 0.0);
            c.RawAuthor = authorInterest.TryGetValue(c.UserId, out var aw) ? aw : 0.0;
            c.RawPopularity = Math.Log(1 + c.Popularity);
            c.Freshness = TimeDecay(now, c.CreatedAt, FreshnessHalfLifeDays);
        }

        var maxHashtag = candidates.Max(c => c.RawHashtag);
        var maxAuthor = candidates.Max(c => c.RawAuthor);
        var maxPopularity = candidates.Max(c => c.RawPopularity);

        foreach (var c in candidates)
        {
            var hashtag = maxHashtag > 0 ? c.RawHashtag / maxHashtag : 0.0;
            var author = maxAuthor > 0 ? c.RawAuthor / maxAuthor : 0.0;
            var popularity = maxPopularity > 0 ? c.RawPopularity / maxPopularity : 0.0;

            c.Score = HashtagScoreWeight * hashtag
                      + AuthorScoreWeight * author
                      + PopularityScoreWeight * popularity
                      + FreshnessScoreWeight * c.Freshness;
        }

        var sorted = candidates
            .OrderByDescending(c => c.Score)
            .ThenByDescending(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToList();

        return Diversify(sorted);
    }

    /// <summary>
    /// Переупорядочивает отсортированный по баллу список так, чтобы не было более
    /// <see cref="MaxConsecutiveSameAuthor"/> постов подряд от одного автора: при достижении лимита
    /// берётся следующий по баллу пост другого автора (жадно), исходный порядок иначе сохраняется.
    /// </summary>
    private static List<int> Diversify(List<CandidateRow> sorted)
    {
        var pending = new LinkedList<CandidateRow>(sorted);
        var result = new List<int>(sorted.Count);

        string? lastAuthor = null;
        var runLength = 0;

        while (pending.Count > 0)
        {
            var node = pending.First;
            LinkedListNode<CandidateRow>? chosen = null;

            while (node is not null)
            {
                var breaksLimit = node.Value.UserId == lastAuthor && runLength >= MaxConsecutiveSameAuthor;
                if (!breaksLimit)
                {
                    chosen = node;
                    break;
                }
                node = node.Next;
            }

            // Если все оставшиеся — тот же автор (лимит нарушат все), берём лучший по баллу.
            chosen ??= pending.First!;

            var picked = chosen.Value;
            pending.Remove(chosen);
            result.Add(picked.Id);

            if (picked.UserId == lastAuthor)
            {
                runLength++;
            }
            else
            {
                lastAuthor = picked.UserId;
                runLength = 1;
            }
        }

        return result;
    }

    /// <summary>Материализует страницу постов по упорядоченным Id, сохраняя порядок ранжирования.</summary>
    private async Task<PagedResponse<List<GetPostDto>>> MaterializePageAsync(
        List<int> orderedIds, string currentId, int page, int size)
    {
        var total = orderedIds.Count;
        var pageIds = orderedIds.Skip((page - 1) * size).Take(size).ToList();

        if (pageIds.Count == 0)
            return new PagedResponse<List<GetPostDto>>(new List<GetPostDto>(), total, page, size);

        var posts = await _context.Posts.AsNoTracking()
            .Where(p => pageIds.Contains(p.Id))
            .Select(PostProjections.ToDto(currentId))
            .ToListAsync();

        // Восстанавливаем порядок скоринга (SQL IN не гарантирует порядок).
        var byId = posts.ToDictionary(p => p.Id);
        var ordered = pageIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();

        await MentionEnrichment.EnrichPostsAsync(_context, ordered);

        return new PagedResponse<List<GetPostDto>>(ordered, total, page, size);
    }

    /// <summary>Батч-загрузка Id хэштегов по постам: <c>postId → список HashtagId</c>.</summary>
    private async Task<Dictionary<int, List<int>>> LoadHashtagIdsByPostAsync(List<int> postIds)
    {
        var rows = await _context.PostHashtags.AsNoTracking()
            .Where(ph => postIds.Contains(ph.PostId))
            .Select(ph => new { ph.PostId, ph.HashtagId })
            .ToListAsync();

        return rows
            .GroupBy(r => r.PostId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.HashtagId).ToList());
    }

    /// <summary>Экспоненциальное затухание: 1.0 сейчас, 0.5 через <paramref name="halfLifeDays"/> дней.</summary>
    private static double TimeDecay(DateTime now, DateTime past, double halfLifeDays)
    {
        var ageDays = (now - past).TotalDays;
        return ageDays <= 0 ? 1.0 : Math.Pow(0.5, ageDays / halfLifeDays);
    }

    /// <summary>Кандидат Explore с сырыми компонентами и итоговым баллом (in-memory ранжирование).</summary>
    private sealed class CandidateRow
    {
        public int Id { get; init; }
        public string UserId { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public int Popularity { get; init; }
        public List<int> HashtagIds { get; init; } = new();

        public double RawHashtag { get; set; }
        public double RawAuthor { get; set; }
        public double RawPopularity { get; set; }
        public double Freshness { get; set; }
        public double Score { get; set; }
    }
}
