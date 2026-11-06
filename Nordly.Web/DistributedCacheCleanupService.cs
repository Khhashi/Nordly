using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nordly.Web;

public sealed class DistributedCacheCleanupService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private readonly PostgresDistributedCache _cache;
    private readonly ILogger<DistributedCacheCleanupService> _logger;

    public DistributedCacheCleanupService(
        PostgresDistributedCache cache,
        ILogger<DistributedCacheCleanupService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);

        do
        {
            try
            {
                var deletedEntries = await _cache.CleanupExpiredEntriesAsync(stoppingToken);
                if (deletedEntries > 0)
                    _logger.LogInformation("Removed {Count} expired distributed cache entries.", deletedEntries);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to remove expired distributed cache entries.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
