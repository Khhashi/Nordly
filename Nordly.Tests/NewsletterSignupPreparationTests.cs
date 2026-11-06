using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Domain.Entities;
using Nordly.Infrastructure.Data;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterSignupPreparationTests
{
    [Test]
    public async Task Valid_Signup_Prepares_Consent_And_Link_Without_Saving()
    {
        await using var db = CreateDatabase();
        var result = await CreatePreparation(db).PrepareAsync(" Customer@Example.com ", true);

        Assert.That(result.Status, Is.EqualTo(NewsletterSignupStatus.Ready));
        Assert.That(result.IsNew, Is.True);
        Assert.That(result.Subscriber!.Email, Is.EqualTo("customer@example.com"));
        Assert.That(result.Subscriber.IsSubscribed, Is.True);
        Assert.That(result.Subscriber.ConsentText, Is.EqualTo(NewsletterSignupInput.ConsentText));
        Assert.That(result.UnsubscribeUrl, Does.StartWith("https://example.com/Newsletter/Unsubscribe?token="));
        Assert.That(await db.NewsletterSubscribers.CountAsync(), Is.Zero);
        Assert.That(db.ChangeTracker.HasChanges(), Is.False);
    }

    [Test]
    public async Task Existing_Active_Subscription_Is_Not_Prepared_Again()
    {
        await using var db = CreateDatabase();
        db.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = "customer@example.com", IsSubscribed = true });
        await db.SaveChangesAsync();

        var result = await CreatePreparation(db).PrepareAsync("customer@example.com", true);
        Assert.That(result.Status, Is.EqualTo(NewsletterSignupStatus.AlreadySubscribed));
        Assert.That(result.Subscriber, Is.Null);
        Assert.That(db.ChangeTracker.HasChanges(), Is.False);
    }

    [Test]
    public async Task Missing_Base_Url_Is_Reported_Without_Changes()
    {
        await using var db = CreateDatabase();
        var result = await CreatePreparation(db, null).PrepareAsync("customer@example.com", true);

        Assert.That(result.Status, Is.EqualTo(NewsletterSignupStatus.InvalidConfiguration));
        Assert.That(await db.NewsletterSubscribers.CountAsync(), Is.Zero);
        Assert.That(db.ChangeTracker.HasChanges(), Is.False);
    }

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
