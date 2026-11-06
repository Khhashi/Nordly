using Microsoft.EntityFrameworkCore;
using Nordly.Domain.Entities;
using Nordly.Infrastructure.Data;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterSignupStoreTests
{
    [Test]
    public async Task Stale_Preparation_Cannot_Overwrite_Concurrent_Activation()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var oldVersion = Guid.NewGuid();
        await using var seed = new OrderDbContext(options);
        seed.NewsletterSubscribers.Add(new NewsletterSubscriber
        {
            Email = "customer@example.com", UnsubscribeVersion = oldVersion
        });
        await seed.SaveChangesAsync();

        await using var staleDb = new OrderDbContext(options);
        var stale = await staleDb.NewsletterSubscribers.AsNoTracking().SingleAsync();
        Assert.That(NewsletterSubscriptionState.TryActivate(stale), Is.True);
        var prepared = new PreparedNewsletterSignup(
            NewsletterSignupStatus.Ready, stale, oldVersion, false, "https://example.com");

        await using var otherDb = new OrderDbContext(options);
        var concurrent = await otherDb.NewsletterSubscribers.SingleAsync();
        Assert.That(NewsletterSubscriptionState.TryActivate(concurrent), Is.True);
        var winningVersion = concurrent.UnsubscribeVersion;
        await otherDb.SaveChangesAsync();

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
            await new NewsletterSignupStore(staleDb).SaveAsync(prepared));
        await using var check = new OrderDbContext(options);
        var saved = await check.NewsletterSubscribers.SingleAsync();
        Assert.That(saved.IsSubscribed, Is.True);
        Assert.That(saved.UnsubscribeVersion, Is.EqualTo(winningVersion));
    }

    [Test]
    public async Task Rejects_Unprepared_Signup()
    {
        await using var db = new OrderDbContext(new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var store = new NewsletterSignupStore(db);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await store.SaveAsync(new PreparedNewsletterSignup(NewsletterSignupStatus.ConsentRequired)));
        Assert.That(await db.NewsletterSubscribers.CountAsync(), Is.Zero);
    }
}
