using Domain.DTOs.Message;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common;

/// <summary>
/// Догрузка реакций (§8) к уже материализованным сообщениям. Реакции полиморфны и не имеют
/// навигации на сообщение, поэтому их нельзя спроецировать внутри EF-выражения — вместо этого
/// батч-запросом собираем карту <c>MessageId → реакции</c> и раскладываем по DTO.
/// </summary>
public static class ReactionEnrichment
{
    /// <summary>Возвращает карту <c>MessageId → список реакций</c> для набора сообщений одного контекста.</summary>
    public static async Task<Dictionary<int, List<MessageReactionDto>>> LoadAsync(
        DataContext context, MessageContext messageContext, IReadOnlyCollection<int> messageIds)
    {
        if (messageIds.Count == 0)
            return new Dictionary<int, List<MessageReactionDto>>();

        var rows = await context.MessageReactions.AsNoTracking()
            .Where(r => r.MessageContext == messageContext && messageIds.Contains(r.MessageId))
            .OrderBy(r => r.CreatedAt)
            .Select(r => new
            {
                r.MessageId,
                r.UserId,
                UserName = r.User!.UserName ?? string.Empty,
                r.Emoji
            })
            .ToListAsync();

        return rows
            .GroupBy(r => r.MessageId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new MessageReactionDto
                {
                    UserId = x.UserId,
                    UserName = x.UserName,
                    Emoji = x.Emoji
                }).ToList());
    }
}
