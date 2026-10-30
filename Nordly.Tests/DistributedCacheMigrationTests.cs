using Microsoft.EntityFrameworkCore;
using Nordly.Infrastructure.Data;
using Npgsql;

namespace Nordly.Tests;

[TestFixture]
public class DistributedCacheMigrationTests
{
    private readonly string? _connectionString = Environment.GetEnvironmentVariable("NORDLY_TEST_MIGRATION_CONNECTION");

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
