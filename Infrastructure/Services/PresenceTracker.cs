using Infrastructure.Services.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// In-memory реализация <see cref="IPresenceTracker"/> (singleton). Хранит на каждого пользователя
/// набор его активных connectionId; онлайн = набор непуст. Доступ синхронизирован простым
/// <c>lock</c> — операции короткие, а состояние эфемерное (переживать рестарт не нужно).
/// </summary>
public class PresenceTracker : IPresenceTracker
{
    private readonly Dictionary<string, HashSet<string>> _connections = new();
    private readonly object _lock = new();

    public bool Connect(string userId, string connectionId)
    {
        lock (_lock)
        {
            if (!_connections.TryGetValue(userId, out var set))
            {
                set = new HashSet<string>();
                _connections[userId] = set;
            }

            var wasOffline = set.Count == 0;
            set.Add(connectionId);
            return wasOffline;
        }
    }

    public bool Disconnect(string userId, string connectionId)
    {
        lock (_lock)
        {
            if (!_connections.TryGetValue(userId, out var set))
                return false;

            set.Remove(connectionId);
            if (set.Count == 0)
            {
                _connections.Remove(userId);
                return true;
            }

            return false;
        }
    }

    public bool IsOnline(string userId)
    {
        lock (_lock)
            return _connections.TryGetValue(userId, out var set) && set.Count > 0;
    }
}
