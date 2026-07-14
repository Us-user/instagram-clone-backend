using Domain.DTOs.Presence;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Абстракция real-time рассылки событий набора (§1). Реализация живёт в WebApi (поверх
/// <c>ChatHub</c>/<c>GroupChatHub</c>), чтобы слой Infrastructure не зависел от WebApi.
/// <see cref="ITypingService"/> вызывает её после проверки прав отправителя.
/// </summary>
public interface ITypingNotifier
{
    /// <summary>Доставить событие набора собеседнику в личном чате.</summary>
    Task NotifyDirectTypingAsync(string recipientUserId, TypingDto typing);

    /// <summary>Доставить актуальный список печатающих остальным участникам группы.</summary>
    Task NotifyGroupTypingAsync(IReadOnlyCollection<string> recipientUserIds, GroupTypingDto typing);
}
