using Domain.DTOs.Presence;

namespace WebApi.Hubs;

/// <summary>Строго типизированные методы, которые сервер вызывает у клиентов presence-хаба.</summary>
public interface IPresenceClient
{
    /// <summary>Доставка изменившегося онлайн-статуса пользователя (онлайн/офлайн + lastSeen).</summary>
    Task ReceivePresence(UserPresenceDto presence);
}
