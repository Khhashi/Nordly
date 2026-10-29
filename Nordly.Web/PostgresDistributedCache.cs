using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Nordly.Infrastructure.Data;
using Npgsql;

namespace Nordly.Web;

public sealed class PostgresDistributedCache : IDistributedCache
{
    private readonly IDbContextFactory<OrderDbContext> _dbContextFactory;

    public PostgresDistributedCache(IDbContextFactory<OrderDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public byte[]? Get(string key)
    {
        return GetAsync(key).GetAwaiter().GetResult();
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(token);
        var entry = await db.DistributedCacheEntries.SingleOrDefaultAsync(item => item.Key == key, token);

        if (entry is null)
            return null;

        var now = DateTimeOffset.UtcNow;
        if (IsExpired(entry, now))
        {
            db.DistributedCacheEntries.Remove(entry);
            await db.SaveChangesAsync(token);
            return null;
        }

        if (entry.SlidingExpiration is not null)
        {
            entry.LastAccessed = now;
            await db.SaveChangesAsync(token);
        }

        return entry.Value;
    }

    public void Refresh(string key)
    {
        RefreshAsync(key).GetAwaiter().GetResult();
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(token);
        var entry = await db.DistributedCacheEntries.SingleOrDefaultAsync(item => item.Key == key, token);
        if (entry is null)
            return;

        var now = DateTimeOffset.UtcNow;
        if (IsExpired(entry, now))
        {
            db.DistributedCacheEntries.Remove(entry);
            await db.SaveChangesAsync(token);
            return;
        }

        if (entry.SlidingExpiration is not null)
        {
            entry.LastAccessed = now;
            await db.SaveChangesAsync(token);
        }
    }

    public void Remove(string key)
    {
        RemoveAsync(key).GetAwaiter().GetResult();
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(token);
        var entry = await db.DistributedCacheEntries.SingleOrDefaultAsync(item => item.Key == key, token);
        if (entry is null)
            return;

        db.DistributedCacheEntries.Remove(entry);
        await db.SaveChangesAsync(token);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        SetAsync(key, value, options).GetAwaiter().GetResult();
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        var now = DateTimeOffset.UtcNow;
        var absoluteExpiration = GetAbsoluteExpiration(options, now);

        for (var attempt = 0; ; attempt++)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(token);
            var entry = await db.DistributedCacheEntries.SingleOrDefaultAsync(item => item.Key == key, token);

            if (entry is null)
            {
                entry = new DistributedCacheEntry { Key = key };
                db.DistributedCacheEntries.Add(entry);
            }

            entry.Value = value;
            entry.AbsoluteExpiration = absoluteExpiration;
            entry.SlidingExpiration = options.SlidingExpiration;
            entry.LastAccessed = now;

            try
            {
                await db.SaveChangesAsync(token);
                return;
            }
            catch (DbUpdateException exception) when (attempt == 0 && IsUniqueConstraintViolation(exception))
            {
            }
        }
    }

    public async Task<int> CleanupExpiredEntriesAsync(CancellationToken token = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(token);
        var now = DateTimeOffset.UtcNow;

        return await db.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM "DistributedCacheEntries"
            WHERE ("AbsoluteExpiration" IS NOT NULL AND "AbsoluteExpiration" <= {now})
               OR ("SlidingExpiration" IS NOT NULL AND "LastAccessed" + "SlidingExpiration" <= {now})
            """, token);
    }

    private static bool IsExpired(DistributedCacheEntry entry, DateTimeOffset now)
    {
        return entry.AbsoluteExpiration <= now
            || (entry.SlidingExpiration is not null
                && entry.LastAccessed.Add(entry.SlidingExpiration.Value) <= now);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
    }

    private static DateTimeOffset? GetAbsoluteExpiration(DistributedCacheEntryOptions options, DateTimeOffset now)
    {
        return options.AbsoluteExpirationRelativeToNow is not null
            ? now.Add(options.AbsoluteExpirationRelativeToNow.Value)
            : options.AbsoluteExpiration;
    }
}
