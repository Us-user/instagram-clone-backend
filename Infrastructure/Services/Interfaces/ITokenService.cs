using Domain.Entities;

namespace Infrastructure.Services.Interfaces;

/// <summary>Генерация access-токена (JWT) для сессии пользователя.</summary>
public interface ITokenService
{
    /// <summary>
    /// Собирает подписанный access-токен (JWT) с claim'ами userId/userName/email/sessionId/роли.
    /// Срок жизни — <c>Jwt:AccessTokenLifetimeMinutes</c> (по ТЗ 15 мин).
    /// </summary>
    string GenerateAccessToken(User user, IEnumerable<string> roles, Guid sessionId);
}
