using Infrastructure.Services.Interfaces;

namespace WebApi.Middleware;

/// <summary>
/// На каждом авторизованном запросе проверяет, что сессия из claim <c>sessionId</c> жива (не отозвана
/// и не истекла), и троттлингом обновляет её <c>LastActivityAt</c>. Отзыв сессии благодаря этому
/// действует мгновенно, а не через 15 минут (срок access-токена). Анонимные запросы и легаси-токены
/// без <c>sessionId</c> пропускаются без проверки. Исключение <see cref="UnauthorizedAccessException"/>
/// обрабатывается глобальным <see cref="ExceptionHandlingMiddleware"/> → 401 в формате Response.
/// </summary>
public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;

    public SessionValidationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserService currentUser,
        ISessionService sessionService)
    {
        if (currentUser.IsAuthenticated && currentUser.SessionId is { } sessionId)
        {
            var valid = await sessionService.ValidateAndTouchAsync(sessionId);
            if (!valid)
                throw new UnauthorizedAccessException("Сессия завершена или истекла. Войдите заново.");
        }

        await _next(context);
    }
}
