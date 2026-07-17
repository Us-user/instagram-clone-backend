namespace Domain.DTOs.Live;

// Компактные полезные нагрузки real-time событий LiveHub (§6). Сгруппированы в одном файле — это
// мелкие DTO-события. Более «богатые» события переиспользуют существующие DTO:
//   NewComment            → LiveCommentDto
//   GuestRequestReceived  → LiveGuestRequestDto
//   StreamEnded.stats     → LiveStatsDto

/// <summary>Событие <c>ViewerCount</c>: актуальное число зрителей в эфире.</summary>
public class LiveViewerCountDto
{
    public int StreamId { get; set; }
    public int Count { get; set; }
}

/// <summary>Событие <c>ViewerLeft</c>/<c>GuestLeft</c>/<c>ViewerBanned</c>/<c>NewLike</c>: только userId.</summary>
public class LiveUserRefDto
{
    public string UserId { get; set; } = string.Empty;
}

/// <summary>Событие <c>CommentPinned</c>/<c>CommentDeleted</c>: id комментария.</summary>
public class LiveCommentRefDto
{
    public int CommentId { get; set; }
}

/// <summary>Событие <c>GuestRequestDeclined</c>: id заявки (только заявителю).</summary>
public class LiveGuestRequestRefDto
{
    public int RequestId { get; set; }
}

/// <summary>Событие <c>StreamEnded</c>: id эфира и итоговая статистика.</summary>
public class LiveStreamEndedDto
{
    public int StreamId { get; set; }
    public LiveStatsDto Stats { get; set; } = new();
}

/// <summary>Событие <c>StreamStarted</c>: эфир начался (подписчикам хоста).</summary>
public class LiveStreamStartedDto
{
    public int StreamId { get; set; }
    public string HostUserId { get; set; } = string.Empty;
    public string HostUserName { get; set; } = string.Empty;
    public string? Title { get; set; }
}
