namespace Domain.Entities;

/// <summary>
/// Бан зрителя в конкретном эфире: хост кикнул его без возможности вернуться. Проверяется при
/// join/комментировании/подаче заявки в гости. Действует в пределах одного эфира.
/// </summary>
public class LiveBan
{
    public int Id { get; set; }

    public int LiveStreamId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime BannedAt { get; set; }

    public LiveStream? LiveStream { get; set; }
    public User? User { get; set; }
}
