namespace Domain.Entities;

/// <summary>
/// Заход зрителя в эфир. При повторном входе создаётся новая запись — «уникальные зрители»
/// считаются как <c>DISTINCT UserId</c>. <see cref="LeftAt"/>/<see cref="WatchDurationSeconds"/>
/// проставляются при выходе (или по грейс-периоду при обрыве соединения / при завершении эфира).
/// </summary>
public class LiveViewer
{
    public int Id { get; set; }

    public int LiveStreamId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; }

    /// <summary>Момент выхода. <c>null</c> — зритель ещё смотрит.</summary>
    public DateTime? LeftAt { get; set; }

    /// <summary>Суммарная длительность просмотра этого захода в секундах (фиксируется при выходе).</summary>
    public int WatchDurationSeconds { get; set; }

    public LiveStream? LiveStream { get; set; }
    public User? User { get; set; }
}
