using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterUnsubscribeLinkTests
{
    [Test]
    public void Missing_Public_Base_Url_Does_Not_Produce_A_Link()
    {
        var link = new NewsletterUnsubscribeLink(new ConfigurationBuilder().Build(),
            new NewsletterUnsubscribeToken(new EphemeralDataProtectionProvider()),
            NullLogger<NewsletterUnsubscribeLink>.Instance);

        Assert.That(link.TryCreate(Guid.NewGuid(), Guid.NewGuid(), out var url), Is.False);
        Assert.That(url, Is.Empty);
    }
}
