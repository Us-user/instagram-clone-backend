using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Сохранение/удаление файлов в <c>wwwroot/images</c>. В БД хранится только имя файла.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Проверяет расширение и размер, сохраняет файл под уникальным Guid-именем и
    /// возвращает это имя. Бросает <see cref="Domain.Exceptions.BadRequestException"/> при нарушении правил.
    /// </summary>
    /// <param name="file">Загружаемый файл.</param>
    /// <param name="allowedExtensions">Разрешённые расширения (с точкой). По умолчанию — изображения.</param>
    /// <param name="maxSizeBytes">Максимальный размер. По умолчанию — 10 МБ.</param>
    Task<string> SaveFileAsync(
        IFormFile file,
        string[]? allowedExtensions = null,
        long? maxSizeBytes = null,
        CancellationToken cancellationToken = default);

    /// <summary>Удаляет файл с диска по имени. Отсутствие файла/имени игнорируется.</summary>
    void DeleteFile(string? fileName);
}
