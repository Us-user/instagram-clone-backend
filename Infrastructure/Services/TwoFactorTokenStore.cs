using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Infrastructure.Services.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// In-memory реализация <see cref="ITwoFactorTokenStore"/>. Токены сессии 2FA хранятся по значению
/// токена, email-коды — по <c>userId</c> (один активный код на пользователя). Просроченные записи
/// удаляются лениво при обращении. Потокобезопасно (<see cref="ConcurrentDictionary{TKey,TValue}"/>).
/// </summary>
public class TwoFactorTokenStore : ITwoFactorTokenStore
{
    private static readonly TimeSpan LoginTokenTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan EmailCodeTtl = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, TokenEntry> _loginTokens = new();
    private readonly ConcurrentDictionary<string, CodeEntry> _emailCodes = new();

    private readonly record struct TokenEntry(string UserId, DateTime ExpiresAt);
    private readonly record struct CodeEntry(string Code, DateTime ExpiresAt);

    public string IssueLoginToken(string userId)
    {
        var token = GenerateToken();
        _loginTokens[token] = new TokenEntry(userId, DateTime.UtcNow + LoginTokenTtl);
        return token;
    }

    public string? PeekLoginToken(string? token)
    {
        if (string.IsNullOrEmpty(token) || !_loginTokens.TryGetValue(token, out var entry))
            return null;

        if (entry.ExpiresAt <= DateTime.UtcNow)
        {
            _loginTokens.TryRemove(token, out _);
            return null;
        }

        return entry.UserId;
    }

    public void InvalidateLoginToken(string? token)
    {
        if (!string.IsNullOrEmpty(token))
            _loginTokens.TryRemove(token, out _);
    }

    public string IssueEmailCode(string userId)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _emailCodes[userId] = new CodeEntry(code, DateTime.UtcNow + EmailCodeTtl);
        return code;
    }

    public bool VerifyAndConsumeEmailCode(string userId, string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || !_emailCodes.TryGetValue(userId, out var entry))
            return false;

        if (entry.ExpiresAt <= DateTime.UtcNow)
        {
            _emailCodes.TryRemove(userId, out _);
            return false;
        }

        if (!FixedEquals(entry.Code, code.Trim()))
            return false;

        _emailCodes.TryRemove(userId, out _);
        return true;
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('='); // url-safe
    }

    private static bool FixedEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
