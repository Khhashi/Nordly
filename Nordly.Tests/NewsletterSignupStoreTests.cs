using Microsoft.EntityFrameworkCore;
using Nordly.Infrastructure.Data;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterSignupStoreTests
{
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
