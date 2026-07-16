namespace Domain.DTOs.Account;

/// <summary>
/// Пара токенов, выдаваемая при успешной аутентификации (login без 2FA, login-2fa, register,
/// refresh-token). Access-токен короткоживущий (JWT), refresh-токен — долгоживущий и показывается
/// клиенту один раз (в БД хранится только его хэш).
/// </summary>
public class AuthResultDto
{
    /// <summary>JWT для авторизации запросов (заголовок <c>Authorization: Bearer</c>).</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Refresh-токен для обновления пары через <c>/Account/refresh-token</c>.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Срок жизни access-токена в секундах.</summary>
    public int ExpiresIn { get; set; }

    /// <summary>Id созданной сессии (совпадает с claim <c>sessionId</c> в access-токене).</summary>
    public Guid SessionId { get; set; }
}
