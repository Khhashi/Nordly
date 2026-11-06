using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Infrastructure.Data;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterSignupPreparationTests
{
    [TestCase("customer@example.com", false, NewsletterSignupStatus.ConsentRequired)]
    [TestCase("not-an-address", true, NewsletterSignupStatus.InvalidEmail)]
    public async Task Invalid_Input_Is_Rejected_Without_Changes(
        string email, bool consent, NewsletterSignupStatus expected)
    {
        await using var db = CreateDatabase();
        var result = await CreatePreparation(db).PrepareAsync(email, consent);

        Assert.That(result.Status, Is.EqualTo(expected));
        Assert.That(result.Subscriber, Is.Null);
        Assert.That(await db.NewsletterSubscribers.CountAsync(), Is.Zero);
        Assert.That(db.ChangeTracker.HasChanges(), Is.False);
    }

    private static OrderDbContext CreateDatabase() =>
        new(new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static NewsletterSignupPreparation CreatePreparation(OrderDbContext db, string? baseUrl = "https://example.com")
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["PublicBaseUrl"] = baseUrl }).Build();
        var tokens = new NewsletterUnsubscribeToken(new EphemeralDataProtectionProvider());
        var links = new NewsletterUnsubscribeLink(config, tokens, NullLogger<NewsletterUnsubscribeLink>.Instance);
        return new NewsletterSignupPreparation(db, links);
    }
}
