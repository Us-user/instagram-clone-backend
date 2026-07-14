using System.Security.Cryptography;
using System.Text;
using Infrastructure.Services.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Реализация TOTP (RFC 6238, HMAC-SHA1, 6 цифр, шаг 30с) поверх <see cref="HMACSHA1"/> и Base32.
/// Внешняя библиотека (Otp.NET) не подключается — алгоритм детерминированный и полностью покрывается
/// стандартной криптографией платформы; секрет хранится в <c>User.TwoFactorSecret</c> (Base32).
/// </summary>
public class TotpService : ITotpService
{
    private const int Digits = 6;
    private const int PeriodSeconds = 30;
    private const int SecretBytes = 20; // 160 бит — рекомендация RFC для HMAC-SHA1

    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(SecretBytes);
        return Base32Encode(bytes);
    }

    public string BuildOtpauthUri(string secret, string account, string issuer)
    {
        var label = Uri.EscapeDataString($"{issuer}:{account}");
        var issuerEnc = Uri.EscapeDataString(issuer);
        return $"otpauth://totp/{label}?secret={secret}&issuer={issuerEnc}" +
               $"&algorithm=SHA1&digits={Digits}&period={PeriodSeconds}";
    }

    public bool VerifyCode(string? secret, string? code, int window = 1)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
            return false;

        code = code.Trim();
        if (code.Length != Digits || !code.All(char.IsDigit))
            return false;

        byte[] key;
        try
        {
            key = Base32Decode(secret);
        }
        catch
        {
            return false;
        }

        var currentStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / PeriodSeconds;
        for (long i = -window; i <= window; i++)
        {
            if (FixedEquals(ComputeCode(key, currentStep + i), code))
                return true;
        }

        return false;
    }

    /// <summary>Вычисляет 6-значный HOTP-код для конкретного шага счётчика (RFC 4226 §5.3/§5.4).</summary>
    private static string ComputeCode(byte[] key, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes); // счётчик передаётся big-endian

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
                     | ((hash[offset + 1] & 0xff) << 16)
                     | ((hash[offset + 2] & 0xff) << 8)
                     | (hash[offset + 3] & 0xff);

        var otp = binary % (int)Math.Pow(10, Digits);
        return otp.ToString().PadLeft(Digits, '0');
    }

    private static bool FixedEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));

    // ── Base32 (RFC 4648, алфавит A–Z2–7, без паддинга) ───────────────────────────
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private static string Base32Encode(byte[] data)
    {
        var sb = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0, bitsLeft = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                sb.Append(Base32Alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }
        if (bitsLeft > 0)
            sb.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 31]);
        return sb.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        input = input.Trim().Replace(" ", "").Replace("=", "").ToUpperInvariant();
        var output = new List<byte>(input.Length * 5 / 8);
        int buffer = 0, bitsLeft = 0;
        foreach (var c in input)
        {
            var val = Base32Alphabet.IndexOf(c);
            if (val < 0)
                throw new FormatException($"Недопустимый символ Base32: '{c}'.");
            buffer = (buffer << 5) | val;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                output.Add((byte)((buffer >> (bitsLeft - 8)) & 0xff));
                bitsLeft -= 8;
            }
        }
        return output.ToArray();
    }
}
