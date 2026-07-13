using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Разбор упоминаний (@username) и проводка их в <see cref="Mention"/> + уведомления
/// <see cref="NotificationType.Mention"/> (Phase 13, §4). Упоминание создаётся только если
/// адресат существует, не в блокировке с автором и разрешает упоминания от автора
/// (<see cref="WhoCanMention"/>). Собственные упоминания игнорируются.
/// </summary>
public class MentionService : IMentionService
{
    private readonly DataContext _context;
    private readonly INotificationService _notifications;

    public MentionService(DataContext context, INotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    public async Task ProcessMentionsAsync(
        string? text, string authorUserId, MentionEntityType entityType, int entityId)
    {
        var usernames = TextParsing.ExtractMentions(text);
        if (usernames.Count == 0)
            return;

        var lowered = usernames.Select(u => u.ToLowerInvariant()).ToList();

        // Разрешаем юзернеймы в существующих юзеров (кроме самого автора) + их настройка упоминаний.
        var candidates = await _context.Users.AsNoTracking()
            .Where(u => u.UserName != null
                && lowered.Contains(u.UserName.ToLower())
                && u.Id != authorUserId)
            .Select(u => new
            {
                u.Id,
                WhoCanMention = _context.PrivacySettings
                    .Where(s => s.UserId == u.Id)
                    .Select(s => (WhoCanMention?)s.WhoCanMention)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var createdUserIds = new List<string>();

        foreach (var candidate in candidates)
        {
            // Блокировка в любую сторону — упоминание не создаётся.
            if (await AccessGuard.IsBlockBetweenAsync(_context, authorUserId, candidate.Id))
                continue;

            // «Кто может упоминать»: Everyone — всегда; Followers — только одобренный подписчик; Nobody — никто.
            var who = candidate.WhoCanMention ?? WhoCanMention.Everyone;
            if (who == WhoCanMention.Nobody)
                continue;
            if (who == WhoCanMention.Followers
                && !await AccessGuard.IsAcceptedFollowerAsync(_context, authorUserId, candidate.Id))
                continue;

            // Не дублируем упоминание одного юзера в одном объекте (пара уникальна в БД).
            var exists = await _context.Mentions.AnyAsync(m =>
                m.MentionedUserId == candidate.Id
                && m.EntityType == entityType
                && m.EntityId == entityId);
            if (exists)
                continue;

            _context.Mentions.Add(new Mention
            {
                MentionedUserId = candidate.Id,
                AuthorUserId = authorUserId,
                EntityType = entityType,
                EntityId = entityId,
                CreatedAt = DateTime.UtcNow
            });
            createdUserIds.Add(candidate.Id);
        }

        if (createdUserIds.Count == 0)
            return;

        await _context.SaveChangesAsync();

        var notificationEntity = ToNotificationEntity(entityType);
        foreach (var userId in createdUserIds)
            await _notifications.CreateAsync(
                userId, authorUserId, NotificationType.Mention, notificationEntity, entityId);
    }

    /// <summary>Маппинг типа объекта упоминания в тип объекта уведомления.</summary>
    private static NotificationEntityType ToNotificationEntity(MentionEntityType entityType) =>
        entityType switch
        {
            MentionEntityType.Post => NotificationEntityType.Post,
            MentionEntityType.Comment => NotificationEntityType.Comment,
            MentionEntityType.StoryReply => NotificationEntityType.Story,
            _ => NotificationEntityType.Post
        };
}
