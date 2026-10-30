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
            return null;

        if (entry.SlidingExpiration is not null)
        {
            entry.LastAccessed = now;
            entry.ExpiresAt = GetEffectiveExpiration(entry.AbsoluteExpiration, entry.SlidingExpiration, now);
            try
            {
                await db.SaveChangesAsync(token);
            }
            catch (DbUpdateConcurrencyException)
            {
                return null;
            }
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
            return;

        if (entry.SlidingExpiration is not null)
        {
            entry.LastAccessed = now;
            entry.ExpiresAt = GetEffectiveExpiration(entry.AbsoluteExpiration, entry.SlidingExpiration, now);
            try
            {
                await db.SaveChangesAsync(token);
            }
            catch (DbUpdateConcurrencyException)
            {
                return;
            }
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
        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
        }
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
            var isNewEntry = entry is null;

            if (entry is null)
            {
                entry = new DistributedCacheEntry { Key = key };
                db.DistributedCacheEntries.Add(entry);
            }

            entry.Value = value;
            entry.AbsoluteExpiration = absoluteExpiration;
            entry.SlidingExpiration = options.SlidingExpiration;
            entry.LastAccessed = now;
            entry.ExpiresAt = GetEffectiveExpiration(absoluteExpiration, options.SlidingExpiration, now);

            if (!isNewEntry)
            {
                db.Entry(entry).Property(item => item.Value).IsModified = true;
                db.Entry(entry).Property(item => item.AbsoluteExpiration).IsModified = true;
                db.Entry(entry).Property(item => item.SlidingExpiration).IsModified = true;
                db.Entry(entry).Property(item => item.LastAccessed).IsModified = true;
                db.Entry(entry).Property(item => item.ExpiresAt).IsModified = true;
            }

            try
            {
                await db.SaveChangesAsync(token);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt == 0)
            {
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
            WHERE "ExpiresAt" IS NOT NULL AND "ExpiresAt" <= {now}
            """, token);
    }

    private static bool IsExpired(DistributedCacheEntry entry, DateTimeOffset now)
    {
        return entry.ExpiresAt <= now;
    }

    private static DateTimeOffset? GetEffectiveExpiration(
        DateTimeOffset? absoluteExpiration,
        TimeSpan? slidingExpiration,
        DateTimeOffset now)
    {
        DateTimeOffset? slidingExpirationAt = slidingExpiration is null
            ? null
            : now.Add(slidingExpiration.Value);
        if (absoluteExpiration is null)
            return slidingExpirationAt;
        if (slidingExpirationAt is null)
            return absoluteExpiration;

        return absoluteExpiration.Value <= slidingExpirationAt.Value
            ? absoluteExpiration
            : slidingExpirationAt;
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
            : options.AbsoluteExpiration?.ToUniversalTime();
    }
}
