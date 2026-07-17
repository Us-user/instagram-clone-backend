using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Фоновая задача автозавершения «висящих» эфиров (требование §9.6): раз в <see cref="Interval"/> находит
/// эфиры в статусе Live без активности дольше <see cref="Inactivity"/> (хост отключился и ничего не
/// происходит) или превысившие <see cref="MaxDuration"/>, и корректно их завершает (закрывает комнату,
/// фиксирует статистику, оповещает зрителей). Обычно эфир завершается вебхуком <c>room_finished</c> —
/// эта задача страхует случаи, когда вебхук не пришёл.
/// </summary>
public class LiveStreamCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Inactivity = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaxDuration = TimeSpan.FromHours(12);

    private readonly IServiceProvider _services;
    private readonly ILogger<LiveStreamCleanupService> _logger;

    public LiveStreamCleanupService(IServiceProvider services, ILogger<LiveStreamCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ILiveStreamService>();
                await service.AutoEndInactiveStreamsAsync(Inactivity, MaxDuration);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Автозавершение эфиров завершилось ошибкой, повтор в следующем цикле.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
