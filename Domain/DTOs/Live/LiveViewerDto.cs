namespace Domain.DTOs.Live;

/// <summary>Текущий зритель эфира (для хоста: <c>get-viewers</c>). Момент захода — <see cref="JoinedAt"/>.</summary>
public class LiveViewerDto
{
    public LiveUserDto User { get; set; } = new();
    public DateTime JoinedAt { get; set; }
}
