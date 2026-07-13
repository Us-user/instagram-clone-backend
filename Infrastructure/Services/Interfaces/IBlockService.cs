using Domain.DTOs.User;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Блокировки пользователей (§6). Текущий юзер (кто блокирует/разблокирует) — из claims.
/// При блокировке обе стороны отписываются друг от друга.
/// </summary>
public interface IBlockService
{
    /// <summary>Заблокировать <paramref name="userId"/> (+ взаимная отписка).</summary>
    Task<Response<bool>> BlockUserAsync(string? userId);

    /// <summary>Снять блокировку с <paramref name="userId"/>.</summary>
    Task<Response<bool>> UnblockUserAsync(string? userId);

    /// <summary>Список заблокированных текущим пользователем (с пагинацией).</summary>
    Task<PagedResponse<List<GetUserDto>>> GetBlockedUsersAsync(int? pageNumber, int? pageSize);
}
