using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Common;

/// <summary>
/// Генерация и хэширование refresh-токенов (модуль сессий). Токен — криптостойкая случайная строка
/// (64 байта из <see cref="RandomNumberGenerator"/>, base64url); высокая энтропия позволяет хранить
/// только SHA-256 (hex) без медленного KDF (как <see cref="BackupCodeHasher"/>). В открытом виде токен
/// показывается клиенту один раз; в логи не пишется.
/// </summary>
public static class RefreshTokenHasher
{
    /// <summary>Генерирует новый refresh-токен в открытом виде (url-safe base64, 64 байта энтропии).</summary>
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('='); // url-safe
    }

    /// <summary>SHA-256 (hex) от токена — то, что хранится в БД.</summary>
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
