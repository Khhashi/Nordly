using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Nordly.Infrastructure.Data;
using Npgsql;
using Nordly.Web;

namespace Nordly.Tests;

public class PostgresDistributedCacheTests
{
    private readonly string? _migrationConnection = Environment.GetEnvironmentVariable("NORDLY_TEST_MIGRATION_CONNECTION");

    [Test]
    public async Task Set_And_Get_Round_Trips_Through_Database_Entries()
    {
        var cache = CreateCache();
        var value = new byte[] { 1, 2, 3, 4 };

        await cache.SetAsync("cart-session", value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });

        var loaded = await cache.GetAsync("cart-session");

        Assert.That(loaded, Is.EqualTo(value));
    }

    [Test]
    public async Task Remove_Deletes_The_Cached_Value()
    {
        var cache = CreateCache();

        await cache.SetAsync("to-remove", new byte[] { 9 }, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        await cache.RemoveAsync("to-remove");

        Assert.That(await cache.GetAsync("to-remove"), Is.Null);
    }

    [Test]
    public void Cache_Can_Be_Resolved_As_A_Singleton_Without_Capturing_A_Scoped_Service()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<OrderDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<IDistributedCache, PostgresDistributedCache>();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.That(provider.GetRequiredService<IDistributedCache>(), Is.InstanceOf<PostgresDistributedCache>());
    }

    [Test]
    public async Task Postgres_Cache_Survives_New_Instance_And_Isolates_Session_Keys()
    {
        await InIsolatedSchema(async connectionString =>
        {
            var factory = new PostgresDbContextFactory(connectionString);
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.MigrateAsync();

            var sessionOneKey = $"session:{Guid.NewGuid():N}:cart";
            var sessionTwoKey = $"session:{Guid.NewGuid():N}:cart";
            var firstInstance = new PostgresDistributedCache(factory);
            await firstInstance.SetAsync(sessionOneKey, new byte[] { 1, 2 }, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
            await firstInstance.SetAsync(sessionTwoKey, new byte[] { 7, 8 }, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            var restartedInstance = new PostgresDistributedCache(new PostgresDbContextFactory(connectionString));

            Assert.That(await restartedInstance.GetAsync(sessionOneKey), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(await restartedInstance.GetAsync(sessionTwoKey), Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(await restartedInstance.GetAsync($"session:{Guid.NewGuid():N}:cart"), Is.Null);
        });
    }

    [Test]
    public async Task Postgres_Cache_Refreshes_Sliding_Expiry_And_Cleans_Expired_Entries()
    {
        await InIsolatedSchema(async connectionString =>
        {
            var factory = new PostgresDbContextFactory(connectionString);
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.MigrateAsync();

            var cache = new PostgresDistributedCache(factory);
            await cache.SetAsync("expired-absolute", new byte[] { 1 }, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(-1)
            });
            await cache.SetAsync("expired-sliding", new byte[] { 2 }, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });
            await cache.SetAsync("active-sliding", new byte[] { 3 }, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });
            var explicitExpiration = new DateTimeOffset(2040, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));
            await cache.SetAsync("offset-expiration", new byte[] { 4 }, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = explicitExpiration
            });

            var now = DateTimeOffset.UtcNow;
            var priorActiveAccess = now.AddMinutes(-4);
            await using (var db = await factory.CreateDbContextAsync())
            {
                var expiredSliding = await db.DistributedCacheEntries.SingleAsync(entry => entry.Key == "expired-sliding");
                expiredSliding.LastAccessed = now.AddMinutes(-6);
                var activeSliding = await db.DistributedCacheEntries.SingleAsync(entry => entry.Key == "active-sliding");
                activeSliding.LastAccessed = priorActiveAccess;
                await db.SaveChangesAsync();
            }

            Assert.That(await cache.GetAsync("active-sliding"), Is.EqualTo(new byte[] { 3 }));
            await using (var db = await factory.CreateDbContextAsync())
            {
                var activeAfterRefresh = await db.DistributedCacheEntries.SingleAsync(entry => entry.Key == "active-sliding");
                Assert.That(activeAfterRefresh.LastAccessed, Is.GreaterThan(priorActiveAccess));
                var savedExpiration = await db.DistributedCacheEntries
                    .Where(entry => entry.Key == "offset-expiration")
                    .Select(entry => entry.AbsoluteExpiration)
                    .SingleAsync();
                Assert.That(savedExpiration!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
                Assert.That(savedExpiration.Value, Is.EqualTo(explicitExpiration.ToUniversalTime()));
            }

            Assert.That(await cache.CleanupExpiredEntriesAsync(), Is.EqualTo(2));
            Assert.That(await cache.GetAsync("expired-absolute"), Is.Null);
            Assert.That(await cache.GetAsync("expired-sliding"), Is.Null);
        });
    }

    [Test]
    public async Task Concurrent_Postgres_Sets_Keep_One_Entry_For_A_Session_Key()
    {
        await InIsolatedSchema(async connectionString =>
        {
            var factory = new PostgresDbContextFactory(connectionString);
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.MigrateAsync();

            var cache = new PostgresDistributedCache(factory);
            var writes = Enumerable.Range(0, 16)
                .Select(value => cache.SetAsync("shared-session-cart", new[] { (byte)value }, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                }));

            await Task.WhenAll(writes);

            var result = await cache.GetAsync("shared-session-cart");
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result![0], Is.InRange((byte)0, (byte)15));

            var concurrentUpdates = Enumerable.Range(16, 16)
                .Select(value => cache.SetAsync("shared-session-cart", new[] { (byte)value }, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                }));
            await Task.WhenAll(concurrentUpdates);

            var updatedResult = await cache.GetAsync("shared-session-cart");
            Assert.That(updatedResult, Has.Length.EqualTo(1));
            Assert.That(updatedResult![0], Is.InRange((byte)16, (byte)31));
            await using var verifyDb = await factory.CreateDbContextAsync();
            Assert.That(await verifyDb.DistributedCacheEntries.CountAsync(entry => entry.Key == "shared-session-cart"), Is.EqualTo(1));
        });
    }

    private static PostgresDistributedCache CreateCache()
    {
        return new PostgresDistributedCache(new TestDbContextFactory());
    }

    private async Task InIsolatedSchema(Func<string, Task> test)
    {
        if (string.IsNullOrWhiteSpace(_migrationConnection))
            Assert.Ignore("Set NORDLY_TEST_MIGRATION_CONNECTION to run PostgreSQL cache integration tests.");

        var schema = $"cache_test_{Guid.NewGuid():N}";
        await using var adminConnection = new NpgsqlConnection(_migrationConnection);
        await adminConnection.OpenAsync();
        await using (var createSchema = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", adminConnection))
            await createSchema.ExecuteNonQueryAsync();

        var scopedConnectionString = new NpgsqlConnectionStringBuilder(_migrationConnection)
        {
            SearchPath = schema
        }.ConnectionString;

        try
        {
            await test(scopedConnectionString);
        }
        finally
        {
            await using var dropSchema = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", adminConnection);
            await dropSchema.ExecuteNonQueryAsync();
        }
    }

    private sealed class TestDbContextFactory : IDbContextFactory<OrderDbContext>
    {
        private readonly DbContextOptions<OrderDbContext> _options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        public OrderDbContext CreateDbContext() => new(_options);

        public Task<OrderDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }

    private sealed class PostgresDbContextFactory : IDbContextFactory<OrderDbContext>
    {
        private readonly DbContextOptions<OrderDbContext> _options;

        public PostgresDbContextFactory(string connectionString)
        {
            var schema = new NpgsqlConnectionStringBuilder(connectionString).SearchPath;
            _options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseNpgsql(connectionString, postgres =>
                    postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .Options;
        }

        public OrderDbContext CreateDbContext() => new(_options);

        public Task<OrderDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }
}
