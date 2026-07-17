namespace Domain.DTOs.Live;

/// <summary>
/// Информация об эфире (для <c>get-stream-by-id</c>, <c>get-active</c>, <c>get-my-streams</c>). Счётчики
/// денормализованы в сущности. <see cref="CurrentViewers"/> — текущее число смотрящих (активные заходы
/// без <c>LeftAt</c>). <see cref="Guests"/> заполняется для карточки эфира по id (иначе <c>null</c>).
/// </summary>
public class LiveStreamDto
{
    public int StreamId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? Title { get; set; }

    /// <summary>Статус эфира строкой: <c>Live</c>/<c>Ended</c>.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Аудитория строкой: <c>All</c>/<c>CloseFriends</c>.</summary>
    public string Audience { get; set; } = string.Empty;

    public LiveUserDto Host { get; set; } = new();

    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    /// <summary>Текущее число зрителей (0 для завершённого эфира).</summary>
    public int CurrentViewers { get; set; }

    public int ViewersPeak { get; set; }
    public int ViewersTotal { get; set; }
    public int CommentsCount { get; set; }
    public int LikesCount { get; set; }

    /// <summary>Текущее число одобренных гостей в эфире.</summary>
    public int GuestsCount { get; set; }

    public bool SavedToStory { get; set; }
    public string? RecordingUrl { get; set; }

    /// <summary>Текущие гости (только в карточке эфира по id; иначе <c>null</c>).</summary>
    public List<LiveUserDto>? Guests { get; set; }
}
