namespace Domain.Enums;

/// <summary>
/// Роль участника в комнате эфира у видео-провайдера (LiveKit). Определяет grants в токене доступа:
/// подписчик только смотрит, publisher — вещает (хост или одобренный гость). Понижение/повышение роли
/// делает бэкенд через <c>IStreamingProvider.UpdateParticipantRoleAsync</c>.
/// </summary>
public enum ParticipantRole
{
    /// <summary>Только смотрит (canSubscribe).</summary>
    Subscriber = 0,

    /// <summary>Вещает — хост или одобренный гость (canPublish + canSubscribe).</summary>
    Publisher = 1
}
