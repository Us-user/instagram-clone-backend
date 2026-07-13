using Domain.DTOs.User;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Поиск пользователей, история текстового поиска и просмотренных профилей,
/// удаление пользователя (только Admin). Id текущего юзера берётся из claims.
/// </summary>
public interface IUserService
{
    /// <summary>Поиск пользователей по userName/email с пагинацией.</summary>
    Task<PagedResponse<List<GetUserDto>>> GetUsersAsync(
        string? userName, string? email, int? pageNumber, int? pageSize);

    // ── История текстового поиска ─────────────────────────────────────────────
    Task<Response<GetSearchHistoryDto>> AddSearchHistoryAsync(string? text);
    Task<Response<List<GetSearchHistoryDto>>> GetSearchHistoriesAsync();
    Task<Response<bool>> DeleteSearchHistoryAsync(int id);
    Task<Response<bool>> DeleteSearchHistoriesAsync();

    // ── История просмотренных профилей ────────────────────────────────────────
    Task<Response<GetUserSearchHistoryDto>> AddUserSearchHistoryAsync(string? userSearchId);
    Task<Response<List<GetUserSearchHistoryDto>>> GetUserSearchHistoriesAsync();
    Task<Response<bool>> DeleteUserSearchHistoryAsync(int id);
    Task<Response<bool>> DeleteUserSearchHistoriesAsync();

    /// <summary>Удаление пользователя (вызывается только из Admin-эндпоинта).</summary>
    Task<Response<bool>> DeleteUserAsync(string? userId);
}
