namespace Infrastructure.Options;

/// <summary>
/// Параметры внешнего хранилища Cloudinary из секции "Cloudinary". Включается, только когда заданы
/// все три ключа (<see cref="IsConfigured"/>) — иначе (в т.ч. локально без секретов) работает
/// дисковый <c>FileService</c> в <c>wwwroot/images</c>. Секрет клиенту не отдаётся.
/// <para>
/// На Render ключи задаются переменными окружения <c>Cloudinary__CloudName</c>,
/// <c>Cloudinary__ApiKey</c>, <c>Cloudinary__ApiSecret</c>; локально — через user-secrets.
/// </para>
/// </summary>
public class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    /// <summary>Имя облака (Cloud name) из дашборда Cloudinary.</summary>
    public string CloudName { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>Корневая папка в Cloudinary, внутрь которой складываются подпапки (images/voice/…).</summary>
    public string Folder { get; set; } = "instaclone";

    /// <summary>Хранилище активно, только когда заданы все три обязательных ключа.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CloudName)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(ApiSecret);
}
