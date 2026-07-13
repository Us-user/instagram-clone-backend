namespace Infrastructure.Constants;

/// <summary>
/// Имена кастомных JWT-claim'ов. Роль кладётся стандартным <see cref="System.Security.Claims.ClaimTypes.Role"/>,
/// чтобы работал <c>[Authorize(Roles = "...")]</c>.
/// </summary>
public static class CustomClaims
{
    public const string UserId = "userId";
    public const string UserName = "userName";
    public const string Email = "email";
}
