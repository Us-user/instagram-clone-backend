using Domain.DTOs.Chat;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Абстракция реал-тайм доставки сообщений чата. Реализация живёт в WebApi (SignalR-хаб),
/// чтобы слой Infrastructure не зависел от WebApi. <see cref="IChatService"/> вызывает её
/// после сохранения сообщения.
/// </summary>
public interface IChatNotifier
{
    /// <summary>Доставить новое сообщение обоим участникам чата в реальном времени.</summary>
    Task NotifyMessageAsync(string user1Id, string user2Id, GetMessageDto message);
}
