using Domain.DTOs.FollowingRelationShip;
using Domain.DTOs.User;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Социальный граф подписок. Текущий юзер (тот, кто подписывается/отписывается) — из claims.
/// С Phase 12 подписка на приватный аккаунт создаёт запрос (Pending) вместо прямой связи;
/// списки учитывают только одобренные (Accepted) связи и скрыты у приватных чужих аккаунтов.
/// </summary>
public interface IFollowingRelationShipService
{
    /// <summary>Подписчики пользователя (одобренные; кто подписан на <paramref name="userId"/>).</summary>
    Task<Response<List<GetUserDto>>> GetSubscribersAsync(string? userId);

    /// <summary>Подписки пользователя (одобренные; на кого подписан <paramref name="userId"/>).</summary>
    Task<Response<List<GetUserDto>>> GetSubscriptionsAsync(string? userId);

    /// <summary>
    /// Текущий юзер подписывается на <paramref name="followingUserId"/>. На публичный аккаунт —
    /// сразу Accepted (+уведомление Follow); на приватный — Pending (+уведомление FollowRequest).
    /// Запрет дубля, подписки на себя и подписки при блокировке.
    /// </summary>
    Task<Response<bool>> AddAsync(string? followingUserId);

    /// <summary>Текущий юзер отписывается от <paramref name="followingUserId"/> (в любом статусе).</summary>
    Task<Response<bool>> DeleteAsync(string? followingUserId);

    /// <summary>Входящие запросы на подписку (Pending) к текущему пользователю, с пагинацией.</summary>
    Task<PagedResponse<List<GetFollowRequestDto>>> GetFollowRequestsAsync(int? pageNumber, int? pageSize);

    /// <summary>Принять запрос от <paramref name="requesterUserId"/> (→ Accepted + уведомление FollowRequestAccepted).</summary>
    Task<Response<bool>> AcceptRequestAsync(string? requesterUserId);

    /// <summary>Отклонить запрос от <paramref name="requesterUserId"/> (удалить Pending-связь).</summary>
    Task<Response<bool>> DeclineRequestAsync(string? requesterUserId);

    /// <summary>Отменить свой исходящий запрос на подписку к <paramref name="followingUserId"/>.</summary>
    Task<Response<bool>> CancelRequestAsync(string? followingUserId);
}
