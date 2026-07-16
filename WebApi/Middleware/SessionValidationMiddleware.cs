using Microsoft.AspNetCore.Authorization;
using Infrastructure.Services.Interfaces;

namespace WebApi.Middleware;

/// <summary>
/// На каждом запросе к <b>защищённому</b> эндпоинту проверяет, что сессия из claim <c>sessionId</c>
/// жива (не отозвана и не истекла), и троттлингом обновляет её <c>LastActivityAt</c>. Отзыв сессии
/// благодаря этому действует мгновенно, а не через 15 минут (срок access-токена).
/// <para>
/// Анонимные (<c>[AllowAnonymous]</c>) эндпоинты — <c>login</c>, <c>register</c>, <c>login-2fa</c>,
/// <c>refresh-token</c> и т.п. — проверку сессии НЕ проходят: даже если клиент прислал ещё-валидный
/// access-токен с уже отозванной сессией, повторный вход/refresh обязаны работать (иначе после
/// logout/отзыва/смены пароля пользователь не смог бы залогиниться заново, пока не истечёт старый
/// токен). Также пропускаются анонимные запросы и легаси-токены без <c>sessionId</c>.
/// </para>
/// Исключение <see cref="UnauthorizedAccessException"/> обрабатывается глобальным
/// <see cref="ExceptionHandlingMiddleware"/> → 401 в формате Response.
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
        // На [AllowAnonymous]-эндпоинтах сессию не валидируем — вход/refresh не должны блокироваться
        // просроченной/отозванной сессией из случайно приложенного токена.
        var allowsAnonymous = context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;

        if (!allowsAnonymous && currentUser.IsAuthenticated && currentUser.SessionId is { } sessionId)
        {
            var valid = await sessionService.ValidateAndTouchAsync(sessionId);
            if (!valid)
                throw new UnauthorizedAccessException("Сессия завершена или истекла. Войдите заново.");
        }

        await _next(context);
    }
}
