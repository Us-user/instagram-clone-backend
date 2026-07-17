using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Data.Seed;

/// <summary>
/// Генератор лёгких <b>SVG-плейсхолдеров</b> для демо-данных: аватары, обложки постов,
/// рилсов и сторис. Файлы самодостаточны (без внешних зависимостей и без сети) и кладутся в
/// <c>{wwwroot}/images</c>, поэтому реально отрисовываются в UI. Цвет градиента детерминирован
/// по seed-строке — один и тот же вход даёт одну и ту же картинку.
/// </summary>
internal static class DemoAssets
{
    /// <summary>Пишет SVG-файл в папку изображений (перезаписывая существующий). Возвращает имя файла.</summary>
    public static string Write(string imagesFolder, string fileName, string svg)
    {
        File.WriteAllText(Path.Combine(imagesFolder, fileName), svg, new UTF8Encoding(false));
        return fileName;
    }

    /// <summary>Квадратный аватар: градиент + инициалы. UI-клиент обрежет по кругу при необходимости.</summary>
    public static string Avatar(string seed, string initials)
    {
        var (h1, h2) = Hues(seed);
        var init = Escape(initials.ToUpperInvariant());
        return $@"<svg xmlns='http://www.w3.org/2000/svg' width='400' height='400' viewBox='0 0 400 400'>
  <defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'>
    <stop offset='0' stop-color='hsl({h1},68%,58%)'/><stop offset='1' stop-color='hsl({h2},70%,46%)'/>
  </linearGradient></defs>
  <rect width='400' height='400' fill='url(#g)'/>
  <text x='200' y='200' font-family='Segoe UI,Arial,sans-serif' font-size='168' font-weight='700'
        fill='#ffffff' fill-opacity='0.95' text-anchor='middle' dominant-baseline='central'>{init}</text>
</svg>";
    }

    /// <summary>Квадратная (1080×1080) обложка обычного поста: градиент, эмодзи-тема, подпись снизу.</summary>
    public static string Post(string seed, string emoji, string caption)
    {
        var (h1, h2) = Hues(seed);
        var cap = Escape(Truncate(caption, 42));
        return $@"<svg xmlns='http://www.w3.org/2000/svg' width='1080' height='1080' viewBox='0 0 1080 1080'>
  <defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'>
    <stop offset='0' stop-color='hsl({h1},64%,56%)'/><stop offset='1' stop-color='hsl({h2},66%,42%)'/>
  </linearGradient></defs>
  <rect width='1080' height='1080' fill='url(#g)'/>
  <circle cx='860' cy='230' r='230' fill='#ffffff' fill-opacity='0.10'/>
  <circle cx='210' cy='910' r='300' fill='#000000' fill-opacity='0.06'/>
  <text x='540' y='470' font-size='300' text-anchor='middle' dominant-baseline='central'>{Escape(emoji)}</text>
  <text x='540' y='830' font-family='Segoe UI,Arial,sans-serif' font-size='50' font-weight='600'
        fill='#ffffff' fill-opacity='0.95' text-anchor='middle'>{cap}</text>
</svg>";
    }

    /// <summary>Вертикальная (1080×1920) обложка рилса: градиент, тема, кнопка play и бейдж REEL.</summary>
    public static string Reel(string seed, string emoji, string caption)
    {
        var (h1, h2) = Hues(seed);
        var cap = Escape(Truncate(caption, 40));
        return $@"<svg xmlns='http://www.w3.org/2000/svg' width='1080' height='1920' viewBox='0 0 1080 1920'>
  <defs><linearGradient id='g' x1='0' y1='0' x2='0.7' y2='1'>
    <stop offset='0' stop-color='hsl({h1},62%,52%)'/><stop offset='1' stop-color='hsl({h2},68%,34%)'/>
  </linearGradient></defs>
  <rect width='1080' height='1920' fill='url(#g)'/>
  <circle cx='880' cy='320' r='300' fill='#ffffff' fill-opacity='0.08'/>
  <text x='540' y='720' font-size='320' text-anchor='middle' dominant-baseline='central'>{Escape(emoji)}</text>
  <circle cx='540' cy='1120' r='120' fill='none' stroke='#ffffff' stroke-opacity='0.9' stroke-width='10'/>
  <polygon points='512,1058 512,1182 618,1120' fill='#ffffff' fill-opacity='0.92'/>
  <rect x='60' y='70' rx='16' width='190' height='70' fill='#000000' fill-opacity='0.35'/>
  <text x='155' y='118' font-family='Segoe UI,Arial,sans-serif' font-size='40' font-weight='700'
        fill='#ffffff' text-anchor='middle'>REEL</text>
  <text x='540' y='1480' font-family='Segoe UI,Arial,sans-serif' font-size='52' font-weight='600'
        fill='#ffffff' fill-opacity='0.95' text-anchor='middle'>{cap}</text>
</svg>";
    }

    /// <summary>Вертикальная (1080×1920) сторис: градиент, тема, текст и @handle автора.</summary>
    public static string Story(string seed, string handle, string text)
    {
        var (h1, h2) = Hues(seed);
        var line = Escape(Truncate(text, 34));
        return $@"<svg xmlns='http://www.w3.org/2000/svg' width='1080' height='1920' viewBox='0 0 1080 1920'>
  <defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'>
    <stop offset='0' stop-color='hsl({h1},66%,54%)'/><stop offset='1' stop-color='hsl({h2},70%,40%)'/>
  </linearGradient></defs>
  <rect width='1080' height='1920' fill='url(#g)'/>
  <circle cx='260' cy='420' r='260' fill='#ffffff' fill-opacity='0.10'/>
  <circle cx='900' cy='1500' r='320' fill='#000000' fill-opacity='0.07'/>
  <text x='540' y='960' font-family='Segoe UI,Arial,sans-serif' font-size='72' font-weight='700'
        fill='#ffffff' text-anchor='middle'>{line}</text>
  <text x='540' y='1080' font-family='Segoe UI,Arial,sans-serif' font-size='44' font-weight='500'
        fill='#ffffff' fill-opacity='0.85' text-anchor='middle'>@{Escape(handle)}</text>
</svg>";
    }

    /// <summary>Две гармоничные HSL-краски, детерминированно выведенные из seed-строки.</summary>
    private static (int H1, int H2) Hues(string seed)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(seed));
        var h1 = bytes[0] * 360 / 256;
        var h2 = (h1 + 35 + bytes[1] % 70) % 360; // сдвиг оттенка для «живого» градиента
        return (h1, h2);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)].TrimEnd() + "…";

    private static string Escape(string s) => s
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
