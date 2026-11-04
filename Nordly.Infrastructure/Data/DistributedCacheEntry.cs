namespace Nordly.Infrastructure.Data;

public sealed class DistributedCacheEntry
{
    public string Key { get; set; } = string.Empty;
    public byte[] Value { get; set; } = Array.Empty<byte>();
    public DateTimeOffset? AbsoluteExpiration { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public DateTimeOffset LastAccessed { get; set; } = DateTimeOffset.UtcNow;
}
