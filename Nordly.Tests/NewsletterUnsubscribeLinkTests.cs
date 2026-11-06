using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterUnsubscribeLinkTests
{
    [Test]
    public void Link_Uses_Configured_Path_And_Contains_Readable_Protected_Token()
    {
        var tokens = new NewsletterUnsubscribeToken(new EphemeralDataProtectionProvider());
        var link = CreateLink("https://example.com/store/", tokens);
        var subscriberId = Guid.NewGuid();
        var version = Guid.NewGuid();

        Assert.That(link.TryCreate(subscriberId, version, out var url), Is.True);
        var uri = new Uri(url);
        Assert.That(uri.GetLeftPart(UriPartial.Path), Is.EqualTo("https://example.com/store/Newsletter/Unsubscribe"));
        var token = QueryHelpers.ParseQuery(uri.Query)["token"].ToString();
        Assert.That(tokens.TryRead(token, out var actualId, out var actualVersion), Is.True);
        Assert.That(actualId, Is.EqualTo(subscriberId));
        Assert.That(actualVersion, Is.EqualTo(version));
    }

    [TestCase("http://example.com")]
    [TestCase("/relative")]
    [TestCase("https://user:password@example.com")]
    [TestCase("https://example.com/?other=value")]
    [TestCase("https://example.com/#fragment")]
    public void Rejects_Unsafe_Public_Base_Urls(string baseUrl)
    {
        var link = CreateLink(baseUrl, new NewsletterUnsubscribeToken(new EphemeralDataProtectionProvider()));

        Assert.That(link.TryCreate(Guid.NewGuid(), Guid.NewGuid(), out var url), Is.False);
        Assert.That(url, Is.Empty);
    }

    private static NewsletterUnsubscribeLink CreateLink(string baseUrl, NewsletterUnsubscribeToken tokens)
    {
        var settings = new Dictionary<string, string?> { ["PublicBaseUrl"] = baseUrl };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new NewsletterUnsubscribeLink(configuration, tokens, NullLogger<NewsletterUnsubscribeLink>.Instance);
    }

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
