using Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Пишет в лог при старте, какое хранилище файлов активно. Нужен из-за тихого фолбэка на дисковый
/// <see cref="FileService"/>: если ключи Cloudinary не заданы (типичная опечатка в env-переменных на
/// хостинге), загрузки молча уходят в <c>wwwroot/images</c> — а на Render free этот диск эфемерный,
/// и картинки пропадают после первого же рестарта без единой ошибки в логах.
/// </summary>
public class StorageStartupLogger : IHostedService
{
    private readonly CloudinaryOptions _options;
    private readonly ILogger<StorageStartupLogger> _logger;

    public StorageStartupLogger(IOptions<CloudinaryOptions> options, ILogger<StorageStartupLogger> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.IsConfigured)
            _logger.LogInformation(
                "Хранилище файлов: Cloudinary (cloud={Cloud}, папка={Folder}).",
                _options.CloudName, _options.Folder);
        else
            _logger.LogWarning(
                "Хранилище файлов: дисковый wwwroot/images (ключи Cloudinary не заданы). " +
                "На PaaS с эфемерным диском (Render free) загруженные файлы пропадут после рестарта. " +
                "Задайте Cloudinary__CloudName / __ApiKey / __ApiSecret.");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
