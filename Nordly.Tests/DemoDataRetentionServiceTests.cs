using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Domain.Entities;
using Nordly.Infrastructure.Data;
using Nordly.Web.Services;
using Npgsql;

namespace Nordly.Tests;

[TestFixture]
public sealed class DemoDataRetentionServiceTests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TestDbContextFactory(DbContextOptions<OrderDbContext> options)
        : IDbContextFactory<OrderDbContext>
    {
        public OrderDbContext CreateDbContext() => new(options);

        public Task<OrderDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    [Test]
    public async Task Cleanup_Deletes_Only_Records_Older_Than_Thirty_Days()
    {
        await DatabaseMigrationTests.InIsolatedSchema(async connectionString =>
        {
            var schema = new NpgsqlConnectionStringBuilder(connectionString).SearchPath;
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseNpgsql(connectionString, postgres =>
                    postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .Options;

            await using (var migrationDb = new OrderDbContext(options))
                await migrationDb.Database.MigrateAsync();

            var now = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
            var expired = now.AddDays(-31);
            var boundary = now.AddDays(-30);
            var recent = now.AddDays(-29);

            await using (var seedDb = new OrderDbContext(options))
            {
                var product = new Product("Retention test product", 100m);
                var expiredOrder = CreateOrder(expired, product);
                var boundaryOrder = CreateOrder(boundary, product);
                var recentOrder = CreateOrder(recent, product);
                seedDb.Products.Add(product);
                seedDb.Users.AddRange(
                    CreateAccount("expired", expired),
                    CreateAccount("boundary", boundary),
                    CreateAccount("recent", recent));
                seedDb.Orders.AddRange(
                    expiredOrder,
                    boundaryOrder,
                    recentOrder);
                seedDb.UserClaims.Add(new IdentityUserClaim<string>
                {
                    UserId = "expired",
                    ClaimType = "demo",
                    ClaimValue = "expired"
                });
                seedDb.NewsletterSubscribers.AddRange(
                    CreateSubscriber("expired@example.test", expired),
                    CreateSubscriber("boundary@example.test", boundary),
                    CreateSubscriber("recent@example.test", recent));
                await seedDb.SaveChangesAsync();
            }

            var service = new DemoDataRetentionService(
                new TestDbContextFactory(options),
                new FixedTimeProvider(now),
                NullLogger<DemoDataRetentionService>.Instance);

            var deleted = await service.DeleteExpiredDataAsync();

            Assert.That(deleted.Orders, Is.EqualTo(1));
            Assert.That(deleted.NewsletterSubscribers, Is.EqualTo(1));
            Assert.That(deleted.Accounts, Is.EqualTo(1));

            await using var verifyDb = new OrderDbContext(options);
            Assert.That(await verifyDb.Orders.CountAsync(), Is.EqualTo(2));
            Assert.That(await verifyDb.OrderLines.CountAsync(), Is.EqualTo(2));
            Assert.That(await verifyDb.Users.Select(account => account.Id).ToArrayAsync(),
                Is.EquivalentTo(new[] { "boundary", "recent" }));
            Assert.That(await verifyDb.UserClaims.CountAsync(), Is.Zero);
            Assert.That(await verifyDb.NewsletterSubscribers.Select(subscriber => subscriber.Email).ToArrayAsync(),
                Is.EquivalentTo(new[] { "boundary@example.test", "recent@example.test" }));
        });
    }

    private static CustomerAccount CreateAccount(string name, DateTimeOffset createdAt) =>
        new()
        {
            Id = name,
            UserName = $"{name}@example.test",
            NormalizedUserName = $"{name.ToUpperInvariant()}@EXAMPLE.TEST",
            Email = $"{name}@example.test",
            NormalizedEmail = $"{name.ToUpperInvariant()}@EXAMPLE.TEST",
            EmailConfirmed = true,
            CreatedAt = createdAt
        };

    private static NewsletterSubscriber CreateSubscriber(string email, DateTimeOffset subscribedAt) =>
        new()
        {
            Email = email,
            SubscribedAt = subscribedAt.UtcDateTime,
            IsSubscribed = true
        };

    private static Order CreateOrder(DateTimeOffset createdAt, Product product)
    {
        var order = new Order { CreatedAt = createdAt.UtcDateTime };
        order.AddProduct(product, 1);
        return order;
    }
}
