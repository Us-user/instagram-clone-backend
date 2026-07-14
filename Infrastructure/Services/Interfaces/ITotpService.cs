namespace Infrastructure.Services.Interfaces;

/// <summary>
/// TOTP по RFC 6238 (§11): генерация секрета, сборка <c>otpauth://</c>-URI и проверка кодов из
/// приложения-аутентификатора (Google Authenticator/Authy). Реализация — на встроенном HMAC-SHA1,
/// без внешних зависимостей; секрет хранится в <see cref="Domain.Entities.User.TwoFactorSecret"/>.
/// </summary>
public interface ITotpService
{
    /// <summary>Генерирует новый случайный секрет (Base32, 160 бит).</summary>
    string GenerateSecret();

    /// <summary>
    /// Собирает <c>otpauth://totp/...</c>-URI для QR-кода (совместим с Google Authenticator/Authy).
    /// </summary>
    string BuildOtpauthUri(string secret, string account, string issuer);

    /// <summary>
    /// Проверяет 6-значный TOTP-код против секрета с допуском ±<paramref name="window"/> шагов по 30с
    /// (компенсация рассинхронизации часов). Возвращает <c>false</c> при пустом секрете/коде.
    /// </summary>
    bool VerifyCode(string? secret, string? code, int window = 1);
}
