using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

/// <summary>
/// Реализация <see cref="IImageUrlBuilder"/>. База URL берётся так: сначала конфиг
/// <c>Files:PublicBaseUrl</c> (напр. <c>https://api.example.com</c> — удобно за прокси/CDN),
/// иначе — из текущего запроса (<c>scheme://host</c> + <c>PathBase</c>). Если ни запроса, ни
/// конфига нет (напр. фоновая задача) — отдаём относительный путь <c>/images/&lt;имя&gt;</c>.
/// Файлы хранятся под Guid-именами (буквы/цифры/точка/дефис) — доп. URL-кодирование не требуется.
/// </summary>
public class ImageUrlBuilder : IImageUrlBuilder
{
    private const string Folder = "images";

    private readonly IHttpContextAccessor _accessor;
    private readonly string? _configuredBase;

    public ImageUrlBuilder(IHttpContextAccessor accessor, IConfiguration configuration)
    {
        _accessor = accessor;
        _configuredBase = configuration["Files:PublicBaseUrl"]?.TrimEnd('/');
    }

    public string? Build(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var baseUrl = ResolveBase();
        return baseUrl is null
            ? $"/{Folder}/{fileName}"
            : $"{baseUrl}/{Folder}/{fileName}";
    }

    public List<string> BuildMany(IEnumerable<string>? fileNames) =>
        fileNames is null
            ? new List<string>()
            : fileNames
                .Select(Build)
                .Where(url => url is not null)
                .Select(url => url!)
                .ToList();

    private string? ResolveBase()
    {
        if (!string.IsNullOrWhiteSpace(_configuredBase))
            return _configuredBase;

        var request = _accessor.HttpContext?.Request;
        if (request is null)
            return null;

        return $"{request.Scheme}://{request.Host}{request.PathBase}";
    }
}
