namespace Nordly.Infrastructure.Data;

public sealed class PasswordlessLoginChallenge
{
    public string NormalizedEmail { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public int FailedAttempts { get; set; }
}
