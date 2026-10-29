using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nordly.Infrastructure.Data;
using Npgsql;

namespace Nordly.Tests;

[TestFixture]
public class DistributedCacheMigrationTests
{
    private readonly string? _connectionString = Environment.GetEnvironmentVariable("NORDLY_TEST_MIGRATION_CONNECTION");
    private const string InitialCacheMigration = "20261028111042_PersistDistributedCache";

    [Test]
    public async Task Empty_Database_Creates_Distributed_Cache_Table_And_Expiry_Index()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            Assert.Ignore("Set NORDLY_TEST_MIGRATION_CONNECTION to run PostgreSQL migration integration tests.");

        await InIsolatedSchema(async scopedConnection =>
        {
            await using (var db = CreateDbContext(scopedConnection))
            {
                await db.Database.MigrateAsync();
                Assert.That(await db.Database.GetPendingMigrationsAsync(), Is.Empty);
                Assert.That(await db.DistributedCacheEntries.CountAsync(), Is.Zero);
            }

            await using var connection = new NpgsqlConnection(scopedConnection);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("""
                SELECT COUNT(*)
                FROM pg_indexes
                WHERE schemaname = current_schema()
                  AND tablename = 'DistributedCacheEntries'
                  AND indexname = 'IX_DistributedCacheEntries_ExpiresAt'
                """, connection);

            Assert.That((long)(await command.ExecuteScalarAsync())!, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Expiry_Migration_Backfills_Existing_Absolute_And_Sliding_Entries()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            Assert.Ignore("Set NORDLY_TEST_MIGRATION_CONNECTION to run PostgreSQL migration integration tests.");

        await InIsolatedSchema(async scopedConnection =>
        {
            var now = DateTimeOffset.UtcNow;
            // PostgreSQL timestamptz stores microseconds, not .NET 100-nanosecond ticks.
            now = now.AddTicks(-(now.Ticks % 10));
            var absoluteExpiration = now.AddHours(3);
            var slidingExpiration = TimeSpan.FromHours(2);

            await using (var db = CreateDbContext(scopedConnection))
                await db.GetService<IMigrator>().MigrateAsync(InitialCacheMigration);

            await using (var connection = new NpgsqlConnection(scopedConnection))
            {
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand("""
                    INSERT INTO "DistributedCacheEntries"
                        ("Key", "Value", "AbsoluteExpiration", "SlidingExpiration", "LastAccessed")
                    VALUES
                        ('absolute-and-sliding', @value, @absoluteExpiration, @slidingExpiration, @lastAccessed),
                        ('absolute-only', @value, @absoluteExpiration, NULL, @lastAccessed),
                        ('sliding-only', @value, NULL, @slidingExpiration, @lastAccessed),
                        ('no-expiration', @value, NULL, NULL, @lastAccessed)
                    """, connection);
                command.Parameters.AddWithValue("value", new byte[] { 1, 2, 3 });
                command.Parameters.AddWithValue("absoluteExpiration", absoluteExpiration);
                command.Parameters.AddWithValue("slidingExpiration", slidingExpiration);
                command.Parameters.AddWithValue("lastAccessed", now);
                await command.ExecuteNonQueryAsync();
            }

            await using (var db = CreateDbContext(scopedConnection))
                await db.Database.MigrateAsync();

            await using var verifyDb = CreateDbContext(scopedConnection);
            var entries = await verifyDb.DistributedCacheEntries
                .ToDictionaryAsync(entry => entry.Key, entry => entry.ExpiresAt);

            Assert.That(entries["absolute-and-sliding"], Is.EqualTo(now.Add(slidingExpiration)));
            Assert.That(entries["absolute-only"], Is.EqualTo(absoluteExpiration));
            Assert.That(entries["sliding-only"], Is.EqualTo(now.Add(slidingExpiration)));
            Assert.That(entries["no-expiration"], Is.Null);
        });
    }

    private static OrderDbContext CreateDbContext(string connectionString)
    {
        var schema = new NpgsqlConnectionStringBuilder(connectionString).SearchPath;
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(connectionString, postgres =>
                postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema))
            .Options;
        return new OrderDbContext(options);
    }

    private async Task InIsolatedSchema(Func<string, Task> test)
    {
        var baseConnectionString = _connectionString;
        if (string.IsNullOrWhiteSpace(baseConnectionString))
            Assert.Ignore("Set NORDLY_TEST_MIGRATION_CONNECTION to run PostgreSQL migration integration tests.");

        var schema = $"cache_migration_test_{Guid.NewGuid():N}";
        await using var adminConnection = new NpgsqlConnection(baseConnectionString);
        await adminConnection.OpenAsync();
        await using (var createSchema = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", adminConnection))
            await createSchema.ExecuteNonQueryAsync();

        var scopedConnectionString = new NpgsqlConnectionStringBuilder(baseConnectionString)
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
}
