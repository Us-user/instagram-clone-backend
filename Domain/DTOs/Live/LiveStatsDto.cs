namespace Domain.DTOs.Live;

/// <summary>
/// Статистика эфира (<c>get-stats</c>, только хост) и полезная нагрузка события <c>StreamEnded</c>.
/// Часть полей осмысленна лишь в определённом режиме: во время эфира — <see cref="CurrentViewers"/>/
/// <see cref="ActiveGuests"/>; после эфира — <see cref="DurationSeconds"/>/<see cref="AverageWatchSeconds"/>/
/// <see cref="TotalWatchSeconds"/>/<see cref="TopCommenters"/>/<see cref="GuestRequestsCount"/>.
/// </summary>
public class LiveStatsDto
{
    public int StreamId { get; set; }

    /// <summary>Статус строкой: <c>Live</c>/<c>Ended</c>.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Текущее число зрителей (актуально во время эфира).</summary>
    public int CurrentViewers { get; set; }

    public int ViewersPeak { get; set; }

    /// <summary>Всего уникальных зрителей (DISTINCT UserId).</summary>
    public int UniqueViewers { get; set; }

    public int CommentsCount { get; set; }
    public int LikesCount { get; set; }

    /// <summary>Текущее число одобренных гостей (актуально во время эфира).</summary>
    public int ActiveGuests { get; set; }

    /// <summary>Число заявок в гости за эфир (после эфира).</summary>
    public int GuestRequestsCount { get; set; }

    /// <summary>Длительность эфира в секундах (после завершения; во время эфира — прошедшее время).</summary>
    public int DurationSeconds { get; set; }

    /// <summary>Средняя длительность просмотра на уникального зрителя, сек (после эфира).</summary>
    public int AverageWatchSeconds { get; set; }

    /// <summary>Суммарное время просмотра всех зрителей, сек (после эфира).</summary>
    public int TotalWatchSeconds { get; set; }

    /// <summary>Топ-комментаторы эфира (после эфира).</summary>
    public List<LiveTopCommenterDto> TopCommenters { get; set; } = new();
}

/// <summary>Строка топа комментаторов эфира: пользователь и число его комментариев.</summary>
public class LiveTopCommenterDto
{
    public LiveUserDto User { get; set; } = new();
    public int CommentsCount { get; set; }
}
