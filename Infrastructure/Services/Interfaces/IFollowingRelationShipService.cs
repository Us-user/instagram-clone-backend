using Domain.DTOs.User;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Социальный граф подписок. Текущий юзер (тот, кто подписывается/отписывается) — из claims.
/// </summary>
public interface IFollowingRelationShipService
{
    /// <summary>Подписчики пользователя (кто подписан на <paramref name="userId"/>).</summary>
    Task<Response<List<GetUserDto>>> GetSubscribersAsync(string? userId);

    /// <summary>Подписки пользователя (на кого подписан <paramref name="userId"/>).</summary>
    Task<Response<List<GetUserDto>>> GetSubscriptionsAsync(string? userId);

    /// <summary>Текущий юзер подписывается на <paramref name="followingUserId"/>. Запрет дубля и подписки на себя.</summary>
    Task<Response<bool>> AddAsync(string? followingUserId);

    /// <summary>Текущий юзер отписывается от <paramref name="followingUserId"/>.</summary>
    Task<Response<bool>> DeleteAsync(string? followingUserId);
}
