using Microsoft.AspNetCore.Identity;

namespace Nordly.Infrastructure.Data;

public sealed class CustomerAccount : IdentityUser
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
