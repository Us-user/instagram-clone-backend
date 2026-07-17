using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Domain.Exceptions;
using Infrastructure.Options;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Реализация <see cref="IFileService"/> поверх Cloudinary. В отличие от дискового
/// <see cref="FileService"/>, в БД сохраняется <b>абсолютный HTTPS-URL</b> ассета (а не имя файла) —
/// он переживает рестарты/редеплои PaaS с эфемерным диском. <see cref="ImageUrlBuilder"/> отдаёт такой
/// URL клиенту как есть. Изображения грузятся как <c>image</c>, прочее (голос/видео/файлы) — с
/// авто-определением типа ресурса. Проверки расширения/размера сохранены из дисковой версии.
/// </summary>
public class CloudinaryFileService : IFileService
{
    private const long DefaultMaxSizeBytes = 10 * 1024 * 1024; // 10 МБ

    private static readonly string[] DefaultImageExtensions =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

    private readonly Cloudinary _cloudinary;
    private readonly string _baseFolder;

    public CloudinaryFileService(Cloudinary cloudinary, IOptions<CloudinaryOptions> options)
    {
        _cloudinary = cloudinary;
        _baseFolder = string.IsNullOrWhiteSpace(options.Value.Folder)
            ? "instaclone"
            : options.Value.Folder.Trim('/');
    }

    public async Task<string> SaveFileAsync(
        IFormFile file,
        string[]? allowedExtensions = null,
        long? maxSizeBytes = null,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("Файл не передан или пуст.");

        var maxSize = maxSizeBytes ?? DefaultMaxSizeBytes;
        if (file.Length > maxSize)
            throw new BadRequestException($"Размер файла превышает допустимый ({maxSize / 1024 / 1024} МБ).");

        var extensions = allowedExtensions ?? DefaultImageExtensions;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !extensions.Contains(extension))
            throw new BadRequestException(
                $"Недопустимое расширение файла. Разрешены: {string.Join(", ", extensions)}.");

        var cloudFolder = ResolveFolder(folder);
        await using var stream = file.OpenReadStream();
        var description = new FileDescription(file.FileName, stream);

        UploadResult result = DefaultImageExtensions.Contains(extension)
            ? await _cloudinary.UploadAsync(new ImageUploadParams
            {
                File = description,
                Folder = cloudFolder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            }, cancellationToken)
            : await _cloudinary.UploadAsync(new RawUploadParams
            {
                File = description,
                Folder = cloudFolder,
                UseFilename = false,
                UniqueFilename = true
            }, "auto", cancellationToken);

        if (result.Error is not null)
            throw new BadRequestException($"Не удалось загрузить файл в хранилище: {result.Error.Message}");

        var url = result.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(url))
            throw new BadRequestException("Хранилище не вернуло URL загруженного файла.");

        return url;
    }

    public void DeleteFile(string? fileName, string? folder = null)
    {
        // В Cloudinary-режиме fileName — это абсолютный URL ассета. Значения не-URL (демо-SVG на диске,
        // легаси-имена) не наши — молча пропускаем, как дисковая версия игнорирует отсутствующий файл.
        if (!TryParseAsset(fileName, out var publicId, out var resourceType))
            return;

        try
        {
            _cloudinary.DestroyAsync(new DeletionParams(publicId) { ResourceType = resourceType })
                .GetAwaiter().GetResult();
        }
        catch
        {
            // best-effort: неудачное удаление ассета не должно ронять удаление сущности.
        }
    }

    public string? CopyFile(string? fileName, string? folder = null)
    {
        // Пересылка (§8): создаём независимую копию ассета, чтобы удаление одного сообщения не забрало
        // файл у другого. Cloudinary скачивает исходник по URL и заливает под новым public_id.
        if (!TryParseAsset(fileName, out _, out var resourceType))
            return fileName; // не наш URL — возвращаем исходное значение без изменений.

        try
        {
            var cloudFolder = ResolveFolder(folder);
            var source = new FileDescription(fileName!); // удалённый URL — Cloudinary заберёт сам.

            UploadResult result = resourceType == ResourceType.Image
                ? _cloudinary.UploadAsync(new ImageUploadParams
                {
                    File = source, Folder = cloudFolder, UseFilename = false, UniqueFilename = true, Overwrite = false
                }).GetAwaiter().GetResult()
                : _cloudinary.UploadAsync(new RawUploadParams
                {
                    File = source, Folder = cloudFolder, UseFilename = false, UniqueFilename = true
                }, "auto").GetAwaiter().GetResult();

            var url = result.Error is null ? result.SecureUrl?.ToString() : null;
            return string.IsNullOrWhiteSpace(url) ? fileName : url; // при неудаче — worst case общий ассет.
        }
        catch
        {
            return fileName;
        }
    }

    private string ResolveFolder(string? folder) =>
        $"{_baseFolder}/{(string.IsNullOrWhiteSpace(folder) ? "images" : folder)}";

    /// <summary>
    /// Извлекает <c>public_id</c> и тип ресурса из URL доставки Cloudinary вида
    /// <c>https://res.cloudinary.com/&lt;cloud&gt;/&lt;image|video|raw&gt;/upload/v123/&lt;folder&gt;/&lt;name&gt;.ext</c>.
    /// Мы никогда не применяем трансформации при загрузке, поэтому между типом доставки и версией нет
    /// лишних сегментов и разбор надёжен. Возвращает <c>false</c>, если строка — не URL Cloudinary.
    /// </summary>
    private static bool TryParseAsset(string? url, out string publicId, out ResourceType resourceType)
    {
        publicId = string.Empty;
        resourceType = ResourceType.Image;

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;
        if (!uri.Host.Contains("cloudinary", StringComparison.OrdinalIgnoreCase))
            return false;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var deliveryIdx = Array.FindIndex(segments,
            s => s is "upload" or "private" or "authenticated");
        if (deliveryIdx <= 0 || deliveryIdx + 1 >= segments.Length)
            return false;

        resourceType = segments[deliveryIdx - 1].ToLowerInvariant() switch
        {
            "video" => ResourceType.Video,
            "raw" => ResourceType.Raw,
            _ => ResourceType.Image
        };

        var rest = segments.Skip(deliveryIdx + 1).ToList();
        if (rest.Count > 0 && rest[0].Length > 1 && rest[0][0] == 'v' && rest[0].Skip(1).All(char.IsDigit))
            rest.RemoveAt(0); // отбрасываем сегмент версии v<цифры>.
        if (rest.Count == 0)
            return false;

        var joined = string.Join('/', rest);
        // Для image/video public_id — без расширения; для raw — с расширением.
        if (resourceType != ResourceType.Raw)
        {
            var dot = joined.LastIndexOf('.');
            if (dot > 0)
                joined = joined[..dot];
        }

        publicId = joined;
        return true;
    }
}
