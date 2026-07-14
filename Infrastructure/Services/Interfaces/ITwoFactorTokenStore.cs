namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Эфемерное хранилище состояния login-флоу 2FA (§11): временные токены сессии (login → login-2fa)
/// и высланные email-коды. Живёт в памяти (singleton) — как presence/typing-трекеры: переживать
/// рестарт не нужно, всё коротко-живущее (TTL ~10 минут). Резервные коды и секрет TOTP — в БД.
/// </summary>
public interface ITwoFactorTokenStore
{
    /// <summary>Выдаёт новый временный токен сессии 2FA для пользователя (TTL ~10 мин).</summary>
    string IssueLoginToken(string userId);

    /// <summary>Возвращает <c>userId</c> по токену, если тот валиден и не истёк, иначе <c>null</c>. Не потребляет токен.</summary>
    string? PeekLoginToken(string? token);

    /// <summary>Инвалидирует токен сессии (после успешного входа).</summary>
    void InvalidateLoginToken(string? token);

    /// <summary>Генерирует и запоминает 6-значный email-код для пользователя (TTL ~10 мин).</summary>
    string IssueEmailCode(string userId);

    /// <summary>Проверяет email-код и потребляет его при совпадении (одноразовый).</summary>
    bool VerifyAndConsumeEmailCode(string userId, string? code);
}
