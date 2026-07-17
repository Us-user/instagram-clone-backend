using Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Streaming;

/// <summary>
/// Пишет в лог при старте, какой видео-провайдер эфиров активен. Нужен из-за тихого фолбэка на
/// <see cref="FakeStreamingProvider"/>: если <c>Streaming:Provider=LiveKit</c> выбран, но хоть один из
/// <c>Url/ApiKey/ApiSecret</c> пуст (типичная опечатка в env-переменных на хостинге), эфиры молча
/// продолжают отдавать фиктивные токены и видео не работает без единой ошибки в логах.
/// </summary>
public class StreamingStartupLogger : IHostedService
{
    private readonly StreamingOptions _options;
    private readonly ILogger<StreamingStartupLogger> _logger;

    public StreamingStartupLogger(IOptions<StreamingOptions> options, ILogger<StreamingStartupLogger> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var wantsLiveKit = string.Equals(_options.Provider, "LiveKit", StringComparison.OrdinalIgnoreCase);
        var configured = !string.IsNullOrWhiteSpace(_options.LiveKit.Url)
                         && !string.IsNullOrWhiteSpace(_options.LiveKit.ApiKey)
                         && !string.IsNullOrWhiteSpace(_options.LiveKit.ApiSecret);

        if (wantsLiveKit && configured)
            _logger.LogInformation("Эфиры: активен провайдер LiveKit ({Url}).", _options.LiveKit.Url);
        else if (wantsLiveKit)
            _logger.LogWarning(
                "Эфиры: выбран Provider=LiveKit, но не заданы {Missing} → откат на Fake-провайдер. " +
                "Токены будут фиктивными, видео НЕ заработает. Задайте Streaming__LiveKit__Url / __ApiKey / __ApiSecret.",
                string.Join(", ", Missing()));
        else
            _logger.LogInformation("Эфиры: активен Fake-провайдер (Provider={Provider}) — видео отключено.", _options.Provider);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private IEnumerable<string> Missing()
    {
        if (string.IsNullOrWhiteSpace(_options.LiveKit.Url)) yield return "Url";
        if (string.IsNullOrWhiteSpace(_options.LiveKit.ApiKey)) yield return "ApiKey";
        if (string.IsNullOrWhiteSpace(_options.LiveKit.ApiSecret)) yield return "ApiSecret";
    }
}
