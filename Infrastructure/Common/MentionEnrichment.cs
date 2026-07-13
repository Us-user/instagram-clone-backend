using Domain.DTOs.Mention;
using Domain.DTOs.Post;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common;

/// <summary>
/// Заполнение списков упомянутых юзеров (<see cref="MentionedUserDto"/>) в уже
/// материализованных DTO постов/комментов (Phase 13). Один батч-запрос к
/// <see cref="DataContext.Mentions"/> по всем Id объектов — чтобы не плодить N+1 и не
/// тянуть упоминания в каждую SQL-проекцию ленты. Общий источник для всех выдач с постами.
/// </summary>
public static class MentionEnrichment
{
    /// <summary>Проставляет <c>MentionedUsers</c> для постов из <paramref name="posts"/>.</summary>
    public static async Task EnrichPostsAsync(DataContext context, List<GetPostDto> posts)
    {
        if (posts.Count == 0)
            return;

        var ids = posts.Select(p => p.Id).ToList();
        var map = await LoadMentionsAsync(context, MentionEntityType.Post, ids);

        foreach (var post in posts)
            if (map.TryGetValue(post.Id, out var users))
                post.MentionedUsers = users;
    }

    /// <summary>Проставляет <c>MentionedUsers</c> для комментов из <paramref name="comments"/>.</summary>
    public static async Task EnrichCommentsAsync(DataContext context, List<GetPostCommentDto> comments)
    {
        if (comments.Count == 0)
            return;

        var ids = comments.Select(c => c.Id).ToList();
        var map = await LoadMentionsAsync(context, MentionEntityType.Comment, ids);

        foreach (var comment in comments)
            if (map.TryGetValue(comment.Id, out var users))
                comment.MentionedUsers = users;
    }

    private static async Task<Dictionary<int, List<MentionedUserDto>>> LoadMentionsAsync(
        DataContext context, MentionEntityType entityType, List<int> entityIds)
    {
        var rows = await context.Mentions.AsNoTracking()
            .Where(m => m.EntityType == entityType && entityIds.Contains(m.EntityId))
            .OrderBy(m => m.Id)
            .Select(m => new
            {
                m.EntityId,
                m.MentionedUserId,
                UserName = m.MentionedUser!.UserName!
            })
            .ToListAsync();

        return rows
            .GroupBy(r => r.EntityId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new MentionedUserDto { Id = r.MentionedUserId, UserName = r.UserName }).ToList());
    }
}
