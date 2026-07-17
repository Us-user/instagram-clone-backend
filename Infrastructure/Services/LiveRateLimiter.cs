using System.Collections.Concurrent;
using Infrastructure.Services.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Скользящее окно на пользователя+эфир: сердечки — до <see cref="MaxLikesPerWindow"/> за
/// <see cref="LikeWindow"/>; комментарии — до <see cref="MaxCommentsPerWindow"/> за <see cref="CommentWindow"/>.
/// Потокобезопасно; редко посещаемые ключи вычищаются лениво при обращении. Singleton.
/// </summary>
public class LiveRateLimiter : ILiveRateLimiter
{
    private const int MaxLikesPerWindow = 5;
    private static readonly TimeSpan LikeWindow = TimeSpan.FromSeconds(1);

    private const int MaxCommentsPerWindow = 5;
    private static readonly TimeSpan CommentWindow = TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<string, Queue<DateTime>> _likes = new();
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _comments = new();

    public bool AllowLike(string userId, int streamId) =>
        Allow(_likes, $"{streamId}:{userId}", MaxLikesPerWindow, LikeWindow);

    public bool AllowComment(string userId, int streamId) =>
        Allow(_comments, $"{streamId}:{userId}", MaxCommentsPerWindow, CommentWindow);

    private static bool Allow(
        ConcurrentDictionary<string, Queue<DateTime>> store, string key, int max, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var queue = store.GetOrAdd(key, _ => new Queue<DateTime>());

        lock (queue)
        {
            while (queue.Count > 0 && now - queue.Peek() > window)
                queue.Dequeue();

            if (queue.Count >= max)
                return false;

            queue.Enqueue(now);
            return true;
        }
    }
}
