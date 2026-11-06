using Microsoft.EntityFrameworkCore;
using Nordly.Domain.Entities;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterSignupPostgresTests
{
    [Test]
    public async Task Stale_Reactivation_Cannot_Overwrite_Concurrent_Activation()
    {
        await DatabaseMigrationTests.InIsolatedSchema(async connectionString =>
        {
            await using var seed = DatabaseMigrationTests.CreateDbContext(connectionString);
            await seed.Database.MigrateAsync();
            seed.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = "customer@example.com" });
            await seed.SaveChangesAsync();

            await using var staleDb = DatabaseMigrationTests.CreateDbContext(connectionString);
            var stale = await staleDb.NewsletterSubscribers.AsNoTracking().SingleAsync();
            var previousVersion = stale.UnsubscribeVersion;
            Assert.That(NewsletterSubscriptionState.TryActivate(stale), Is.True);
            var prepared = new PreparedNewsletterSignup(
                NewsletterSignupStatus.Ready, stale, previousVersion, false, "https://example.com");

            await using var otherDb = DatabaseMigrationTests.CreateDbContext(connectionString);
            var concurrent = await otherDb.NewsletterSubscribers.SingleAsync();
            Assert.That(NewsletterSubscriptionState.TryActivate(concurrent), Is.True);
            var winningVersion = concurrent.UnsubscribeVersion;
            await otherDb.SaveChangesAsync();

            Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
                await new NewsletterSignupStore(staleDb).SaveAsync(prepared));
            await using var check = DatabaseMigrationTests.CreateDbContext(connectionString);
            var saved = await check.NewsletterSubscribers.SingleAsync();
            Assert.That(saved.IsSubscribed, Is.True);
            Assert.That(saved.UnsubscribeVersion, Is.EqualTo(winningVersion));
        });
    }
}
