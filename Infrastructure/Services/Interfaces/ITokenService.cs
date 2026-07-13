using Domain.Entities;

namespace Infrastructure.Services.Interfaces;

/// <summary>Генерация JWT для пользователя.</summary>
public interface ITokenService
{
    /// <summary>Собирает подписанный JWT с claim'ами userId/userName/email/роли.</summary>
    string GenerateToken(User user, IEnumerable<string> roles);
}
