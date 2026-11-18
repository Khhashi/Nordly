using Microsoft.EntityFrameworkCore;
using Nordly.Infrastructure.Data;

namespace Nordly.Web.Services;

public sealed class DemoDataRetentionService(
    IDbContextFactory<OrderDbContext> dbContextFactory,
    TimeProvider timeProvider,
    ILogger<DemoDataRetentionService> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    public async Task<(int Orders, int NewsletterSubscribers, int Accounts)> DeleteExpiredDataAsync(
        CancellationToken cancellationToken = default)
    {
        var cutoff = timeProvider.GetUtcNow() - RetentionPeriod;
        var cutoffDateTime = cutoff.UtcDateTime;

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var deletedOrders = await db.Orders
            .Where(order => order.CreatedAt < cutoffDateTime)
            .ExecuteDeleteAsync(cancellationToken);
        var deletedSubscribers = await db.NewsletterSubscribers
            .Where(subscriber => subscriber.SubscribedAt < cutoffDateTime)
            .ExecuteDeleteAsync(cancellationToken);
        var deletedAccounts = await db.Users
            .Where(account => account.CreatedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return (deletedOrders, deletedSubscribers, deletedAccounts);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);
        do
        {
            try
            {
                var deleted = await DeleteExpiredDataAsync(stoppingToken);
                var total = deleted.Orders + deleted.NewsletterSubscribers + deleted.Accounts;
                if (total > 0)
                {
                    logger.LogInformation(
                        "Removed expired demo records: {Orders} orders, {NewsletterSubscribers} newsletter records, {Accounts} accounts.",
                        deleted.Orders,
                        deleted.NewsletterSubscribers,
                        deleted.Accounts);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to remove expired demo records.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
