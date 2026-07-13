using System.Text.RegularExpressions;

namespace Infrastructure.Common;

/// <summary>
/// Парсинг хэштегов (<c>#tag</c>) и упоминаний (<c>@username</c>) из текста поста/коммента
/// (Phase 13). Хэштеги нормализуются в нижний регистр; упоминания возвращаются как есть
/// (сопоставление с реальными юзернеймами — на стороне сервиса, регистронезависимо).
/// </summary>
public static partial class TextParsing
{
    // Тег: буквы/цифры/подчёркивание (в т.ч. кириллица через \p{L}), минимум один символ.
    [GeneratedRegex(@"#([\p{L}\p{N}_]+)", RegexOptions.Compiled)]
    private static partial Regex HashtagRegex();

    // Юзернейм: латиница/цифры/подчёркивание, точки только между символами (не в конце).
    [GeneratedRegex(@"@([A-Za-z0-9_]+(?:\.[A-Za-z0-9_]+)*)", RegexOptions.Compiled)]
    private static partial Regex MentionRegex();

    /// <summary>
    /// Извлекает нормализованные (lowercase) хэштеги из текста без дубликатов, сохраняя
    /// порядок первого появления.
    /// </summary>
    public static List<string> ExtractHashtags(string? text) =>
        Extract(HashtagRegex(), text, m => m.Groups[1].Value.ToLowerInvariant());

    /// <summary>
    /// Извлекает юзернеймы упоминаний (без <c>@</c>) из текста без дубликатов
    /// (регистронезависимо), сохраняя порядок первого появления.
    /// </summary>
    public static List<string> ExtractMentions(string? text) =>
        Extract(MentionRegex(), text, m => m.Groups[1].Value);

    private static List<string> Extract(Regex regex, string? text, Func<Match, string> selector)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
            return result;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in regex.Matches(text))
        {
            var value = selector(match);
            if (value.Length > 0 && seen.Add(value))
                result.Add(value);
        }

        return result;
    }
}
