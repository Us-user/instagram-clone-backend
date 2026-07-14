using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Common;

/// <summary>
/// Генерация и хэширование одноразовых резервных кодов 2FA (§11). Коды — высокоэнтропийные (не пароли),
/// поэтому достаточно SHA-256 (hex) без медленного KDF. Перед хэшированием код нормализуется (только
/// буквы/цифры, верхний регистр), чтобы ввод с дефисами/в другом регистре совпадал с сохранённым хэшем.
/// </summary>
public static class BackupCodeHasher
{
    // Без визуально неоднозначных символов (0/O, 1/I/L) — коды диктуются/переписываются вручную.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    /// <summary>Генерирует пачку из <paramref name="count"/> кодов формата <c>XXXX-XXXX</c>.</summary>
    public static List<string> GenerateCodes(int count)
    {
        var codes = new List<string>(count);
        for (var i = 0; i < count; i++)
            codes.Add(GenerateOne());
        return codes;
    }

    /// <summary>SHA-256 (hex) от нормализованного кода — то, что хранится в БД.</summary>
    public static string Hash(string code)
    {
        var normalized = Normalize(code);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    private static string Normalize(string? code) =>
        new string((code ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static string GenerateOne()
    {
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(chars, 0, 4) + "-" + new string(chars, 4, 4);
    }
}
