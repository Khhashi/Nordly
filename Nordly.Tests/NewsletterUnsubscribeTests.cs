using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nordly.Domain.Entities;
using Nordly.Infrastructure.Data;
using Nordly.Web.Pages.Newsletter;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterUnsubscribeTests
{
    private static readonly NewsletterUnsubscribeToken TokenService = new(new EphemeralDataProtectionProvider());

    [Test]
    public async Task Unsubscribe_Requires_Confirmation_And_Disables_The_Subscription()
    {
        await using var db = CreateDatabase();
        var subscriber = new NewsletterSubscriber
        {
            Email = "customer@example.com",
            IsSubscribed = true,
            UnsubscribeVersion = Guid.NewGuid()
        };
        var token = TokenService.Create(subscriber.Id, subscriber.UnsubscribeVersion.Value);
        db.NewsletterSubscribers.Add(subscriber);
        await db.SaveChangesAsync();
        var model = CreateModel(db);

        await model.OnGetAsync(token);
        Assert.That(model.CanUnsubscribe, Is.True);
        Assert.That(subscriber.IsSubscribed, Is.True);
        Assert.That(model.Response.Headers.CacheControl.ToString(), Is.EqualTo("no-store"));
        Assert.That(model.Response.Headers["Referrer-Policy"].ToString(), Is.EqualTo("no-referrer"));

        await model.OnPostAsync();

        Assert.That(model.IsUnsubscribed, Is.True);
        Assert.That(subscriber.IsSubscribed, Is.False);
        Assert.That(subscriber.UnsubscribedAt, Is.Not.Null);
        Assert.That(subscriber.UnsubscribeVersion, Is.Null);
        var reusedLink = CreateModel(db);
        await reusedLink.OnGetAsync(token);
        Assert.That(reusedLink.CanUnsubscribe, Is.False);
    }

    [Test]
    public async Task Invalid_Or_Revoked_Token_Cannot_Unsubscribe()
    {
        await using var db = CreateDatabase();
        var subscriber = new NewsletterSubscriber
        {
            Email = "customer@example.com",
            IsSubscribed = true,
            UnsubscribeVersion = Guid.NewGuid()
        };
        db.NewsletterSubscribers.Add(subscriber);
        await db.SaveChangesAsync();
        var model = CreateModel(db);

        await model.OnGetAsync("invalid-token");

        Assert.That(model.CanUnsubscribe, Is.False);
        Assert.That(await db.NewsletterSubscribers.AnyAsync(item => item.IsSubscribed), Is.True);
    }

    [Test]
    public async Task Reissued_Link_For_Active_Subscription_Works()
    {
        await using var db = CreateDatabase();
        var subscriber = new NewsletterSubscriber
        {
            Email = "customer@example.com",
            IsSubscribed = true,
            UnsubscribeVersion = Guid.NewGuid()
        };
        db.NewsletterSubscribers.Add(subscriber);
        await db.SaveChangesAsync();
        var token = TokenService.Create(subscriber.Id, subscriber.UnsubscribeVersion.Value);
        var model = CreateModel(db);

        await model.OnGetAsync(token);
        await model.OnPostAsync();

        Assert.That(model.IsUnsubscribed, Is.True);
        Assert.That(subscriber.IsSubscribed, Is.False);
    }

    [Test]
    public async Task Old_Tracked_Subscription_Cannot_Overwrite_New_Subscription_Version()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var subscriber = new NewsletterSubscriber
        {
            Email = "customer@example.com",
            IsSubscribed = true,
            UnsubscribeVersion = Guid.NewGuid()
        };
        await using var oldContext = new OrderDbContext(options);
        oldContext.NewsletterSubscribers.Add(subscriber);
        await oldContext.SaveChangesAsync();
        await using var newContext = new OrderDbContext(options);
        var renewedSubscriber = await newContext.NewsletterSubscribers.SingleAsync();
        renewedSubscriber.UnsubscribeVersion = Guid.NewGuid();
        await newContext.SaveChangesAsync();

        subscriber.IsSubscribed = false;
        subscriber.UnsubscribeVersion = null;
        Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () => await oldContext.SaveChangesAsync());
        Assert.That((await newContext.NewsletterSubscribers.SingleAsync()).IsSubscribed, Is.True);
    }

    private static OrderDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }

    private static UnsubscribeModel CreateModel(OrderDbContext db)
    {
        var httpContext = new DefaultHttpContext();
        return new UnsubscribeModel(db, TokenService)
        {
            PageContext = new PageContext { HttpContext = httpContext }
        };
    }
}
