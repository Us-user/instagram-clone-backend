using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Административные действия (§10) — только для роли Admin: верификация («синяя галочка»)
/// и управление ролью Admin. Целевой пользователь — из query-параметра <c>userId</c>.
/// </summary>
public interface IAdminService
{
    /// <summary>Поставить пользователю <paramref name="userId"/> верификацию (синюю галочку).</summary>
    Task<Response<bool>> VerifyUserAsync(string? userId);

    /// <summary>Снять верификацию с пользователя <paramref name="userId"/>.</summary>
    Task<Response<bool>> UnverifyUserAsync(string? userId);

    /// <summary>Выдать пользователю <paramref name="userId"/> роль Admin.</summary>
    Task<Response<bool>> GrantAdminAsync(string? userId);

    /// <summary>Снять с пользователя <paramref name="userId"/> роль Admin.</summary>
    Task<Response<bool>> RevokeAdminAsync(string? userId);
}
