using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Сохранение/удаление/копирование файлов в <c>wwwroot/{folder}</c> (по умолчанию <c>images</c>;
/// голосовые — <c>voice</c>). В БД хранится только имя файла.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Проверяет расширение и размер, сохраняет файл под уникальным Guid-именем в
    /// <c>wwwroot/{folder}</c> и возвращает это имя. Бросает
    /// <see cref="Domain.Exceptions.BadRequestException"/> при нарушении правил.
    /// </summary>
    /// <param name="file">Загружаемый файл.</param>
    /// <param name="allowedExtensions">Разрешённые расширения (с точкой). По умолчанию — изображения.</param>
    /// <param name="maxSizeBytes">Максимальный размер. По умолчанию — 10 МБ.</param>
    /// <param name="folder">Подпапка внутри wwwroot. По умолчанию — <c>images</c>.</param>
    Task<string> SaveFileAsync(
        IFormFile file,
        string[]? allowedExtensions = null,
        long? maxSizeBytes = null,
        string? folder = null,
        CancellationToken cancellationToken = default);

    /// <summary>Удаляет файл с диска по имени из <c>wwwroot/{folder}</c>. Отсутствие файла/имени игнорируется.</summary>
    void DeleteFile(string? fileName, string? folder = null);

    /// <summary>
    /// Копирует существующий файл под новым Guid-именем в той же папке и возвращает новое имя
    /// (для пересылки — §8). Если исходного файла нет — возвращает <c>null</c>.
    /// </summary>
    string? CopyFile(string? fileName, string? folder = null);
}
