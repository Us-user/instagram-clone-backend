using Domain.DTOs.CloseFriend;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// «Близкие друзья» (§9): владелец списка — текущий юзер (Id из claims). Добавлять можно любого
/// пользователя (в т.ч. не подписчика), кроме себя и заблокированных. Список сторис
/// <c>CloseFriends</c> виден только тем, кто в этом списке у автора.
/// </summary>
public interface ICloseFriendService
{
    /// <summary>Добавить пользователя в близкие друзья текущего юзера (идемпотентно).</summary>
    Task<Response<string>> AddAsync(string? userId);

    /// <summary>Убрать пользователя из близких друзей текущего юзера (идемпотентно).</summary>
    Task<Response<string>> RemoveAsync(string? userId);

    /// <summary>Список близких друзей текущего юзера (с пагинацией).</summary>
    Task<PagedResponse<List<CloseFriendDto>>> GetListAsync(int? pageNumber, int? pageSize);
}
