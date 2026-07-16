using System.Collections.Concurrent;
using Infrastructure.Services.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// In-memory троттлинг записи <c>LastActivityAt</c>: помнит момент последней фиксации по каждой
/// сессии и разрешает следующую не раньше, чем через <see cref="Window"/>. Потокобезопасно
/// (<see cref="ConcurrentDictionary{TKey,TValue}"/>), эфемерно — переживать рестарт не требуется.
/// </summary>
public class SessionActivityThrottle : ISessionActivityThrottle
{
    /// <summary>Минимальный интервал между записями активности одной сессии в БД.</summary>
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<Guid, DateTime> _lastPersisted = new();

    public bool ShouldPersist(Guid sessionId, DateTime utcNow)
    {
        var updated = false;

        _lastPersisted.AddOrUpdate(
            sessionId,
            _ =>
            {
                updated = true;
                return utcNow;
            },
            (_, previous) =>
            {
                if (utcNow - previous >= Window)
                {
                    updated = true;
                    return utcNow;
                }
                return previous;
            });

        return updated;
    }

    public void Forget(Guid sessionId) => _lastPersisted.TryRemove(sessionId, out _);
}
