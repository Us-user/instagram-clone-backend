using Domain.DTOs.Presence;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Абстракция real-time рассылки изменений присутствия (§1). Реализация живёт в WebApi
/// (SignalR-хаб <c>PresenceHub</c>), чтобы слой Infrastructure не зависел от WebApi.
/// <see cref="IPresenceService"/> вызывает её при переходе пользователя онлайн/офлайн.
/// </summary>
public interface IPresenceNotifier
{
    /// <summary>Разослать изменившийся статус пользователя заинтересованным получателям.</summary>
    Task NotifyPresenceAsync(IReadOnlyCollection<string> recipientUserIds, UserPresenceDto presence);
}
