using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Прямой эфир (модуль эфиров). Хост (<see cref="UserId"/>) вещает через внешний WebRTC-сервер
/// (LiveKit); бэкенд ведёт всю бизнес-логику вокруг эфира и хранит денормализованные счётчики
/// (<see cref="ViewersPeak"/>/<see cref="ViewersTotal"/>/<see cref="LikesCount"/>/<see cref="CommentsCount"/>),
/// обновляемые инкрементами. <see cref="RoomName"/> — уникальное имя комнаты у провайдера (<c>live_{guid}</c>).
/// </summary>
public class LiveStream
{
    public int Id { get; set; }

    /// <summary>Хост эфира (FK на AspNetUsers).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Заголовок эфира (необязательный).</summary>
    public string? Title { get; set; }

    /// <summary>Уникальное имя комнаты у видео-провайдера, напр. <c>live_{guid}</c>.</summary>
    public string RoomName { get; set; } = string.Empty;

    public LiveStreamStatus Status { get; set; } = LiveStreamStatus.Live;

    /// <summary>Аудитория эфира: все или только близкие друзья хоста.</summary>
    public LiveStreamAudience Audience { get; set; } = LiveStreamAudience.All;

    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    /// <summary>Пиковое одновременное число зрителей (денормализовано, инкрементами).</summary>
    public int ViewersPeak { get; set; }

    /// <summary>Всего заходов зрителей за эфир (денормализовано; уникальные считаются отдельно по DISTINCT).</summary>
    public int ViewersTotal { get; set; }

    public int CommentsCount { get; set; }
    public int LikesCount { get; set; }

    /// <summary>Сохранён ли завершённый эфир в сторис.</summary>
    public bool SavedToStory { get; set; }

    /// <summary>URL записи эфира у провайдера (если велась запись). Нужен для сохранения в сторис.</summary>
    public string? RecordingUrl { get; set; }

    public User? User { get; set; }
    public List<LiveViewer> Viewers { get; set; } = new();
    public List<LiveComment> Comments { get; set; } = new();
    public List<LiveLike> Likes { get; set; } = new();
    public List<LiveGuestRequest> GuestRequests { get; set; } = new();
    public List<LiveBan> Bans { get; set; } = new();
}
