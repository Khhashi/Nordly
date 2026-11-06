using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Domain.Entities;
using Nordly.Infrastructure.Data;
using Nordly.Web.Pages.Newsletter;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterUnsubscribeTests
{
    [Test]
    public async Task Valid_Link_Requires_Confirmation_And_Is_Revoked_Afterward()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new OrderDbContext(options);
        var tokens = new NewsletterUnsubscribeToken(new EphemeralDataProtectionProvider());
        var subscriber = new NewsletterSubscriber
        {
            Email = "customer@example.com", IsSubscribed = true, UnsubscribeVersion = Guid.NewGuid()
        };
        db.NewsletterSubscribers.Add(subscriber);
        await db.SaveChangesAsync();
        var token = tokens.Create(subscriber.Id, subscriber.UnsubscribeVersion.Value);
        var model = new UnsubscribeModel(db, tokens, NullLogger<UnsubscribeModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };
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
        await model.OnGetAsync(token);
        Assert.That(model.CanUnsubscribe, Is.False);
    }
}
