namespace Domain.Entities;

/// <summary>
/// Комментарий в прямом эфире. Доставляется в реальном времени через <c>LiveHub</c> и сохраняется
/// для истории (догрузка / просмотр после эфира). Удаление — мягкое (<see cref="IsDeleted"/>);
/// одновременно закреплён может быть только один комментарий (<see cref="IsPinned"/>).
/// </summary>
public class LiveComment
{
    public int Id { get; set; }

    public int LiveStreamId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsPinned { get; set; }
    public bool IsDeleted { get; set; }

    public LiveStream? LiveStream { get; set; }
    public User? User { get; set; }
}
