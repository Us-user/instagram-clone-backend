using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Фоновая очистка сессий: раз в сутки удаляет из БД истёкшие (<c>ExpiresAt &lt; UtcNow</c>) и давно
/// отозванные (<c>IsRevoked</c> и <c>RevokedAt</c> старше 30 дней) сессии. Отозванные держим ещё 30
/// дней — чтобы reuse-detection мог распознать предъявление токена от недавно завершённой сессии.
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);
    private static readonly TimeSpan RevokedRetention = TimeSpan.FromDays(30);

    private readonly IServiceProvider _services;
    private readonly ILogger<SessionCleanupService> _logger;

    public SessionCleanupService(IServiceProvider services, ILogger<SessionCleanupService> logger)
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
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Очистка сессий завершилась ошибкой, повтор в следующем цикле.");
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

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var now = DateTime.UtcNow;
        var revokedCutoff = now - RevokedRetention;

        var deleted = await context.UserSessions
            .Where(s => s.ExpiresAt < now || (s.IsRevoked && s.RevokedAt != null && s.RevokedAt < revokedCutoff))
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
            _logger.LogInformation("Очистка сессий: удалено {Count} записей.", deleted);
    }
}
