namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Доступ к текущему пользователю из JWT-claim'ов (не из параметров запроса).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Id текущего пользователя или <c>null</c>, если запрос анонимный.</summary>
    string? UserId { get; }

    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);

    /// <summary>Id текущего пользователя; бросает 401, если запрос не аутентифицирован.</summary>
    string GetRequiredUserId();
}
