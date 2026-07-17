using System.Collections.Concurrent;
using Infrastructure.Services.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// In-memory реализация <see cref="ILiveConnectionTracker"/>. Держит карту connectionId → (эфир, зритель).
/// Потокобезопасна (<see cref="ConcurrentDictionary{TKey,TValue}"/>). Живёт как singleton между всеми
/// соединениями; переживать рестарт не нужно — «смотрит» определяется живыми соединениями.
/// </summary>
public class LiveConnectionTracker : ILiveConnectionTracker
{
    private readonly ConcurrentDictionary<string, (int StreamId, string UserId)> _connections = new();

    public void Add(string connectionId, int streamId, string userId) =>
        _connections[connectionId] = (streamId, userId);

    public (int StreamId, string UserId)? Remove(string connectionId) =>
        _connections.TryRemove(connectionId, out var mapping) ? mapping : null;

    public bool IsUserWatching(int streamId, string userId) =>
        _connections.Values.Any(m => m.StreamId == streamId && m.UserId == userId);
}
