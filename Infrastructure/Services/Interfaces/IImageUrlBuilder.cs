namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Строит абсолютный URL картинки из имени файла (в БД хранится только имя). Источник хоста —
/// текущий HTTP-запрос (<c>scheme://host</c>) либо явно заданный <c>Files:PublicBaseUrl</c>
/// (для деплоя за прокси). Используется для необязательных <c>*Url</c>-полей в DTO — контракт
/// с именами файлов при этом не ломается.
/// </summary>
public interface IImageUrlBuilder
{
    /// <summary>Абсолютный URL картинки в <c>/images</c>. Возвращает <c>null</c> для пустого имени.</summary>
    string? Build(string? fileName);

    /// <summary>URL'ы для набора имён файлов (пустые/отсутствующие пропускаются).</summary>
    List<string> BuildMany(IEnumerable<string>? fileNames);
}
