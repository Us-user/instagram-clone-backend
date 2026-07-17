using Domain.Enums;

namespace Domain.DTOs.Live;

/// <summary>
/// Тело запроса старта эфира: <c>POST /Live/start</c> — <c>{ title, audience }</c>. Оба поля
/// необязательны: без заголовка эфир безымянный, без аудитории — <see cref="LiveStreamAudience.All"/>.
/// <see cref="Audience"/> биндится числом (0 = All, 1 = CloseFriends).
/// </summary>
public class StartLiveDto
{
    /// <summary>Заголовок эфира (необязательный).</summary>
    public string? Title { get; set; }

    /// <summary>Аудитория: <c>All</c> (0, по умолчанию) или <c>CloseFriends</c> (1).</summary>
    public LiveStreamAudience? Audience { get; set; }
}
