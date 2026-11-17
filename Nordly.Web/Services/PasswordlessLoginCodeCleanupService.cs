using Microsoft.EntityFrameworkCore;
using Nordly.Infrastructure.Data;

namespace Nordly.Web.Services;

public sealed class PasswordlessLoginCodeCleanupService(
    IDbContextFactory<OrderDbContext> dbContextFactory,
    TimeProvider timeProvider,
    ILogger<PasswordlessLoginCodeCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);
        do
        {
            try
            {
                await using var db = await dbContextFactory.CreateDbContextAsync(stoppingToken);
                var deletedCount = await db.PasswordlessLoginChallenges
                    .Where(challenge => challenge.ExpiresAt <= timeProvider.GetUtcNow())
                    .ExecuteDeleteAsync(stoppingToken);
                if (deletedCount > 0)
                    logger.LogInformation("Removed {Count} expired passwordless login challenges.", deletedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to remove expired passwordless login challenges.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
