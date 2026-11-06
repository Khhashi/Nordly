using Microsoft.AspNetCore.DataProtection;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterUnsubscribeTokenTests
{
    [Test]
    public void Protected_Token_Round_Trips_And_Can_Be_Reissued()
    {
        var tokens = new NewsletterUnsubscribeToken(new EphemeralDataProtectionProvider());
        var subscriberId = Guid.NewGuid();
        var version = Guid.NewGuid();

        var first = tokens.Create(subscriberId, version);
        var reissued = tokens.Create(subscriberId, version);

        Assert.That(tokens.TryRead(first, out var firstId, out var firstVersion), Is.True);
        Assert.That(tokens.TryRead(reissued, out var secondId, out var secondVersion), Is.True);
        Assert.That(firstId, Is.EqualTo(subscriberId));
        Assert.That(firstVersion, Is.EqualTo(version));
        Assert.That(secondId, Is.EqualTo(subscriberId));
        Assert.That(secondVersion, Is.EqualTo(version));
        Assert.That(first, Does.Not.Contain(subscriberId.ToString("N")));
    }

    [Test]
    public void Invalid_And_Other_Key_Tokens_Are_Rejected()
    {
        var tokens = new NewsletterUnsubscribeToken(new EphemeralDataProtectionProvider());
        var otherTokens = new NewsletterUnsubscribeToken(new EphemeralDataProtectionProvider());
        var token = otherTokens.Create(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(tokens.TryRead(null, out _, out _), Is.False);
        Assert.That(tokens.TryRead("invalid-token", out _, out _), Is.False);
        Assert.That(tokens.TryRead(new string('a', 1025), out _, out _), Is.False);
        Assert.That(tokens.TryRead(token, out _, out _), Is.False);
    }
}
