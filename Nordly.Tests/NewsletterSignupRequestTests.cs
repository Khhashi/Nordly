using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Infrastructure.Data;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterSignupRequestTests
{
    [Test]
    public async Task Database_Failure_Shows_Error_Without_Sending_Email()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new NewsletterSignupServiceTests.FailSaveInterceptor())
            .Options;
        await using var db = new OrderDbContext(options);
        var sender = new NewsletterSignupServiceTests.RecordingSender(true);
        var service = NewsletterSignupServiceTests.CreateService(db, sender);
        var request = new NewsletterSignupRequest(service, NullLogger<NewsletterSignupRequest>.Instance);

        var message = await request.HandleAsync("customer@example.com", true);

        Assert.That(message.Key, Is.EqualTo("NewsletterError"));
        Assert.That(message.Text, Does.Contain("ikke fullføres"));
        Assert.That(sender.Calls, Is.Zero);
        await using var check = new OrderDbContext(options);
        Assert.That(await check.NewsletterSubscribers.CountAsync(), Is.Zero);
    }
}
