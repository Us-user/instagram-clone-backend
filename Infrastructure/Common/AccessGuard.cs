using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common;

/// <summary>
/// Кросс-срезовые проверки доступа Phase 12: блокировки (в обе стороны) и приватность
/// аккаунтов. Собраны в одном месте, чтобы ленты/поиск/подписки/чат применяли одинаковые
/// правила. Методы-запросы возвращают <see cref="IQueryable{T}"/>/предикаты, пригодные для
/// трансляции в SQL (подзапросы к <see cref="DataContext.Blocks"/>/подпискам).
/// </summary>
public static class AccessGuard
{
    /// <summary>
    /// Id пользователей, с которыми у <paramref name="userId"/> есть блокировка в любую сторону
    /// (он заблокировал их либо они его). Подзапрос для фильтрации выдач списков/поиска.
    /// </summary>
    public static IQueryable<string> BlockRelatedUserIds(DataContext context, string userId) =>
        context.Blocks
            .Where(b => b.BlockerUserId == userId || b.BlockedUserId == userId)
            .Select(b => b.BlockerUserId == userId ? b.BlockedUserId : b.BlockerUserId);

    /// <summary>Есть ли блокировка между <paramref name="a"/> и <paramref name="b"/> (в любую сторону).</summary>
    public static Task<bool> IsBlockBetweenAsync(DataContext context, string a, string b) =>
        context.Blocks.AnyAsync(x =>
            (x.BlockerUserId == a && x.BlockedUserId == b) ||
            (x.BlockerUserId == b && x.BlockedUserId == a));

    /// <summary>Является ли <paramref name="followerId"/> одобренным подписчиком <paramref name="targetId"/>.</summary>
    public static Task<bool> IsAcceptedFollowerAsync(DataContext context, string followerId, string targetId) =>
        context.FollowingRelationShips.AnyAsync(f =>
            f.UserId == followerId &&
            f.FollowingUserId == targetId &&
            f.Status == FollowStatus.Accepted);

    /// <summary>
    /// Может ли <paramref name="currentUserId"/> видеть контент <paramref name="targetUserId"/>
    /// (посты, сторис, списки подписок). Свой контент виден всегда; при блокировке в любую
    /// сторону — нет; приватный чужой аккаунт виден только одобренному подписчику.
    /// </summary>
    public static async Task<bool> CanViewContentAsync(
        DataContext context, string targetUserId, string currentUserId)
    {
        if (targetUserId == currentUserId)
            return true;

        if (await IsBlockBetweenAsync(context, targetUserId, currentUserId))
            return false;

        var isPrivate = await context.Users.AsNoTracking()
            .Where(u => u.Id == targetUserId)
            .Select(u => u.IsPrivate)
            .FirstOrDefaultAsync();

        if (!isPrivate)
            return true;

        return await IsAcceptedFollowerAsync(context, currentUserId, targetUserId);
    }

    /// <summary>
    /// Оставляет в ленте только посты, доступные <paramref name="currentId"/>: свои; либо
    /// авторов без блокировки в любую сторону И (публичных, либо приватных, на которых
    /// текущий подписан одобренно). Применяется ко всем агрегирующим лентам постов.
    /// </summary>
    public static IQueryable<Post> VisibleTo(
        this IQueryable<Post> posts, DataContext context, string currentId) =>
        posts.Where(p =>
            p.UserId == currentId ||
            (!context.Blocks.Any(b =>
                    (b.BlockerUserId == currentId && b.BlockedUserId == p.UserId) ||
                    (b.BlockerUserId == p.UserId && b.BlockedUserId == currentId))
             && (!p.User!.IsPrivate
                 || context.FollowingRelationShips.Any(f =>
                     f.UserId == currentId &&
                     f.FollowingUserId == p.UserId &&
                     f.Status == FollowStatus.Accepted))));
}
