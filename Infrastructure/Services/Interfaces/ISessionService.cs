using Domain.DTOs.Account;
using Domain.DTOs.Session;
using Domain.Entities;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Управление активными сессиями и refresh-токенами. Создаёт сессию при логине (access + refresh),
/// ротирует refresh с reuse-detection, отзывает сессии (свою/все/все-кроме-текущей) и отдаёт список
/// активных сессий текущего юзера. Id текущего юзера/сессии — из claims.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Создаёт новую сессию для пользователя и выдаёт пару токенов. При <paramref name="notifyOnNewDevice"/>
    /// и входе с ранее не встречавшегося устройства/IP создаёт уведомление <c>NewLogin</c> (не при первом
    /// входе — когда прежних сессий нет). Соблюдает лимит <c>MaxActiveSessionsPerUser</c> (отзывает старейшую).
    /// </summary>
    Task<AuthResultDto> CreateSessionAsync(User user, IEnumerable<string> roles, bool notifyOnNewDevice);

    /// <summary>Валидирует refresh-токен, ротирует его (с reuse-detection) и выдаёт новую пару токенов.</summary>
    Task<Response<AuthResultDto>> RefreshAsync(RefreshTokenDto dto);

    /// <summary>Отзывает текущую сессию (по <c>sessionId</c> из claims) — «выход».</summary>
    Task<Response<string>> LogoutAsync();

    /// <summary>Список активных сессий текущего юзера (текущая первой, далее по активности убыв.).</summary>
    Task<Response<List<SessionDto>>> GetActiveSessionsAsync();

    /// <summary>Отзывает конкретную сессию текущего юзера (нельзя завершить чужую).</summary>
    Task<Response<bool>> RevokeSessionAsync(Guid? sessionId);

    /// <summary>Отзывает все сессии текущего юзера, кроме текущей («выйти на всех других устройствах»).</summary>
    Task<Response<bool>> RevokeAllOthersAsync();

    /// <summary>
    /// Проверяет, что сессия жива (не отозвана и не истекла); при валидности троттлингом обновляет
    /// <c>LastActivityAt</c>. Возвращает <c>false</c>, если сессию нужно отклонить (401). Для middleware.
    /// </summary>
    Task<bool> ValidateAndTouchAsync(Guid sessionId);

    /// <summary>Отзывает все активные сессии пользователя (смена/сброс пароля, компрометация, бан).</summary>
    Task RevokeAllForUserAsync(string userId);

    /// <summary>
    /// Отзывает все активные сессии пользователя, кроме текущей (из claims). Если текущей сессии нет —
    /// отзывает все. Для смены пароля/отключения 2FA под авторизацией.
    /// </summary>
    Task RevokeAllOtherForCurrentAsync(string userId);
}
