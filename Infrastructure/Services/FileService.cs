using Domain.Exceptions;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

/// <summary>Хранит файлы в <c>{WebRootPath}/{folder}</c> (по умолчанию <c>images</c>) под уникальными Guid-именами.</summary>
public class FileService : IFileService
{
    private const string DefaultFolder = "images";
    private const long DefaultMaxSizeBytes = 10 * 1024 * 1024; // 10 МБ

    private static readonly string[] DefaultImageExtensions =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

    private readonly IWebHostEnvironment _environment;

    public FileService(IWebHostEnvironment environment) => _environment = environment;

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

        var folderPath = GetFolderPath(folder);
        Directory.CreateDirectory(folderPath);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folderPath, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return fileName;
    }

    public void DeleteFile(string? fileName, string? folder = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        var fullPath = Path.Combine(GetFolderPath(folder), fileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public string? CopyFile(string? fileName, string? folder = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var folderPath = GetFolderPath(folder);
        var source = Path.Combine(folderPath, fileName);
        if (!File.Exists(source))
            return null;

        var extension = Path.GetExtension(fileName);
        var newName = $"{Guid.NewGuid():N}{extension}";
        File.Copy(source, Path.Combine(folderPath, newName));

        return newName;
    }

    private string GetFolderPath(string? folder)
    {
        // В dev WebRootPath может быть null, если wwwroot ещё не создан — берём ContentRoot/wwwroot.
        var webRoot = _environment.WebRootPath
                      ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, string.IsNullOrWhiteSpace(folder) ? DefaultFolder : folder);
    }
}
