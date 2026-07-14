using Domain.DTOs.Presence;
using Infrastructure.Services.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// In-memory реализация <see cref="ITypingTracker"/> (singleton). На каждую группу держит карту
/// «пользователь → (имя, kind, время протухания)». Клиент шлёт событие набора с троттлингом
/// (~раз в несколько секунд) и авто-сбрасывает индикатор ~3 сек; серверный TTL чуть больше, чтобы
/// список нескольких печатающих оставался согласованным между событиями. Протухшие записи
/// вычищаются лениво при каждом обновлении.
/// </summary>
public class TypingTracker : ITypingTracker
{
    private const int TtlSeconds = 6;

    private sealed record Entry(string UserName, string Kind, DateTime ExpiresAt);

    private readonly Dictionary<int, Dictionary<string, Entry>> _groups = new();
    private readonly object _lock = new();

    public List<TypingUserDto> Update(int groupChatId, string userId, string userName, string kind)
    {
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            if (!_groups.TryGetValue(groupChatId, out var map))
            {
                map = new Dictionary<string, Entry>();
                _groups[groupChatId] = map;
            }

            map[userId] = new Entry(userName, kind, now.AddSeconds(TtlSeconds));

            // Ленивая чистка протухших записей.
            var expired = map.Where(kv => kv.Value.ExpiresAt <= now).Select(kv => kv.Key).ToList();
            foreach (var key in expired)
                map.Remove(key);

            if (map.Count == 0)
            {
                _groups.Remove(groupChatId);
                return new List<TypingUserDto>();
            }

            return map
                .Select(kv => new TypingUserDto
                {
                    UserId = kv.Key,
                    UserName = kv.Value.UserName,
                    Kind = kv.Value.Kind
                })
                .ToList();
        }
    }
}
