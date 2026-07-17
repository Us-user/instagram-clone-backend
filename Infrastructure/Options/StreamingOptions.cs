namespace Infrastructure.Options;

/// <summary>
/// Параметры модуля эфиров из секции "Streaming" appsettings.json. Провайдер выбирается по
/// <see cref="Provider"/> (<c>LiveKit</c> или <c>Fake</c>). Один и тот же код работает и с
/// LiveKit Cloud, и с self-hosted — меняются только <see cref="LiveKitOptions.Url"/> и ключи.
/// </summary>
public class StreamingOptions
{
    public const string SectionName = "Streaming";

    /// <summary>Активный провайдер: <c>LiveKit</c> (реальный) или <c>Fake</c> (заглушка для dev/тестов).</summary>
    public string Provider { get; set; } = "Fake";

    public LiveKitOptions LiveKit { get; set; } = new();

    /// <summary>Максимум одновременных гостей в эфире (всего участников — на одного больше, с хостом).</summary>
    public int MaxGuests { get; set; } = 3;

    /// <summary>Максимальная длина текста комментария в эфире.</summary>
    public int MaxCommentLength { get; set; } = 200;
}

/// <summary>Параметры подключения к LiveKit (Cloud или self-hosted). Секретный ключ клиенту не отдаётся.</summary>
public class LiveKitOptions
{
    /// <summary>WebSocket-URL сервера LiveKit, напр. <c>wss://your-project.livekit.cloud</c>.</summary>
    public string Url { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>Срок жизни токена доступа (минуты).</summary>
    public int TokenLifetimeMinutes { get; set; } = 360;
}
