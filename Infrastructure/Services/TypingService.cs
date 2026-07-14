using Domain.DTOs.Presence;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Реализация обработки событий набора (§1). Личный чат — точечная доставка собеседнику с
/// проверкой участия и блокировки; группа — обновление эфемерного списка печатающих
/// (<see cref="ITypingTracker"/>) и рассылка его остальным участникам. Ничего не сохраняет в БД.
/// </summary>
public class TypingService : ITypingService
{
    private readonly DataContext _context;
    private readonly ITypingTracker _tracker;
    private readonly ITypingNotifier _notifier;

    public TypingService(DataContext context, ITypingTracker tracker, ITypingNotifier notifier)
    {
        _context = context;
        _tracker = tracker;
        _notifier = notifier;
    }

    public async Task NotifyDirectTypingAsync(string currentUserId, string currentUserName, int chatId, string? kind)
    {
        if (string.IsNullOrWhiteSpace(currentUserId) || chatId <= 0)
            return;

        var chat = await _context.Chats.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == chatId);
        if (chat is null || (chat.User1Id != currentUserId && chat.User2Id != currentUserId))
            return; // не участник — не сигналим

        var otherId = chat.User1Id == currentUserId ? chat.User2Id : chat.User1Id;

        // При блокировке в любую сторону события набора не доставляем.
        if (await AccessGuard.IsBlockBetweenAsync(_context, currentUserId, otherId))
            return;

        var dto = new TypingDto
        {
            ChatId = chatId,
            UserId = currentUserId,
            UserName = currentUserName,
            Kind = NormalizeKind(kind)
        };

        await _notifier.NotifyDirectTypingAsync(otherId, dto);
    }

    public async Task NotifyGroupTypingAsync(string currentUserId, string currentUserName, int groupChatId, string? kind)
    {
        if (string.IsNullOrWhiteSpace(currentUserId) || groupChatId <= 0)
            return;

        var isMember = await _context.GroupChatMembers
            .AnyAsync(m => m.GroupChatId == groupChatId && m.UserId == currentUserId);
        if (!isMember)
            return;

        // Обновляем эфемерный список печатающих и получаем актуальный набор.
        var typers = _tracker.Update(groupChatId, currentUserId, currentUserName, NormalizeKind(kind));

        var recipients = await _context.GroupChatMembers.AsNoTracking()
            .Where(m => m.GroupChatId == groupChatId && m.UserId != currentUserId)
            .Select(m => m.UserId)
            .ToListAsync();
        if (recipients.Count == 0)
            return;

        var dto = new GroupTypingDto { GroupChatId = groupChatId, Typers = typers };
        await _notifier.NotifyGroupTypingAsync(recipients, dto);
    }

    /// <summary>Нормализует kind к контрактным значениям: <c>voice</c> при записи голосового, иначе <c>text</c>.</summary>
    private static string NormalizeKind(string? kind) =>
        string.Equals(kind?.Trim(), "voice", StringComparison.OrdinalIgnoreCase) ? "voice" : "text";
}
