using System.Security.Claims;
using Infrastructure.Constants;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

/// <summary>Читает данные текущего пользователя из <see cref="HttpContext.User"/>.</summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirstValue(CustomClaims.UserId);
    public string? UserName => Principal?.FindFirstValue(CustomClaims.UserName);
    public string? Email => Principal?.FindFirstValue(CustomClaims.Email);
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    public string GetRequiredUserId() =>
        UserId ?? throw new UnauthorizedAccessException("Пользователь не аутентифицирован.");
}
