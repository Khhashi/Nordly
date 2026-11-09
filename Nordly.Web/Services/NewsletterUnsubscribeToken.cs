using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Nordly.Web.Services;

public sealed class NewsletterUnsubscribeToken(IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector("Nordly.Newsletter.Unsubscribe.v1");

    public string Create(Guid subscriberId, Guid version) =>
        _protector.Protect($"{subscriberId:N}:{version:N}");

    public bool TryRead(string? token, out Guid subscriberId, out Guid version)
    {
        subscriberId = Guid.Empty;
        version = Guid.Empty;
        if (string.IsNullOrEmpty(token) || token.Length > 1024)
        {
            return false;
        }

        try
        {
            var parts = _protector.Unprotect(token).Split(':');
            return parts.Length == 2
                && Guid.TryParseExact(parts[0], "N", out subscriberId)
                && Guid.TryParseExact(parts[1], "N", out version);
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            return false;
        }
    }
}
