using Microsoft.EntityFrameworkCore;
using Nordly.Domain.Entities;
using Nordly.Web.Services;
using Npgsql;

namespace Nordly.Tests;

public class NewsletterSignupPostgresTests
{
    [Test]
    public async Task Concurrent_New_Subscriptions_Respect_Unique_Email_Index()
    {
        await DatabaseMigrationTests.InIsolatedSchema(async connectionString =>
        {
            await using var seed = DatabaseMigrationTests.CreateDbContext(connectionString);
            await seed.Database.MigrateAsync();
            var first = new NewsletterSubscriber { Email = "customer@example.com" };
            var second = new NewsletterSubscriber { Email = "customer@example.com" };
            Assert.That(NewsletterSubscriptionState.TryActivate(first), Is.True);
            Assert.That(NewsletterSubscriptionState.TryActivate(second), Is.True);
            var firstSignup = new PreparedNewsletterSignup(
                NewsletterSignupStatus.Ready, first, null, true, "https://example.com");
            var secondSignup = new PreparedNewsletterSignup(
                NewsletterSignupStatus.Ready, second, null, true, "https://example.com");
            await using var firstDb = DatabaseMigrationTests.CreateDbContext(connectionString);
            await using var secondDb = DatabaseMigrationTests.CreateDbContext(connectionString);

            await new NewsletterSignupStore(firstDb).SaveAsync(firstSignup);
            var exception = Assert.ThrowsAsync<DbUpdateException>(async () =>
                await new NewsletterSignupStore(secondDb).SaveAsync(secondSignup));
            Assert.That(exception!.InnerException, Is.TypeOf<PostgresException>());
            var postgres = (PostgresException)exception.InnerException!;
            Assert.That(postgres.SqlState, Is.EqualTo(PostgresErrorCodes.UniqueViolation));
            Assert.That(postgres.ConstraintName, Is.EqualTo("IX_NewsletterSubscribers_Email"));

            await using var check = DatabaseMigrationTests.CreateDbContext(connectionString);
            var saved = await check.NewsletterSubscribers.SingleAsync();
            Assert.That(saved.Id, Is.EqualTo(first.Id));
            Assert.That(saved.UnsubscribeVersion, Is.EqualTo(first.UnsubscribeVersion));
        });
    }

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
