using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nordly.Infrastructure.Data;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterSignupServiceTests
{
    [Test]
    public async Task Database_Failure_Does_Not_Send_A_Welcome_Email()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new FailSaveInterceptor())
            .Options;
        await using var db = new OrderDbContext(options);
        var sender = new RecordingSender(true);
        var service = CreateService(db, sender);

        Assert.ThrowsAsync<DbUpdateException>(async () =>
            await service.SubscribeAsync("customer@example.com", true));

        Assert.That(sender.Calls, Is.Zero);
        Assert.That(sender.Url, Is.Null);
        Assert.That(db.ChangeTracker.HasChanges(), Is.True);
        await using var check = new OrderDbContext(options);
        Assert.That(await check.NewsletterSubscribers.CountAsync(), Is.Zero);
    }

    internal sealed class FailSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("Database unavailable.");
    }

    [TestCase("customer@example.com", false, NewsletterSignupOutcome.ConsentRequired)]
    [TestCase("not-an-address", true, NewsletterSignupOutcome.InvalidEmail)]
    public async Task Rejected_Input_Does_Not_Save_Or_Send(
        string email, bool consent, NewsletterSignupOutcome expected)
    {
        await using var db = NewsletterSignupPreparationTests.CreateDatabase();
        var sender = new RecordingSender(true);

        var outcome = await CreateService(db, sender).SubscribeAsync(email, consent);

        Assert.That(outcome, Is.EqualTo(expected));
        Assert.That(await db.NewsletterSubscribers.CountAsync(), Is.Zero);
        Assert.That(sender.Calls, Is.Zero);
    }

    [Test]
    public async Task Duplicate_Subscription_Does_Not_Send_Another_Email()
    {
        await using var db = NewsletterSignupPreparationTests.CreateDatabase();
        var sender = new RecordingSender(true);
        var service = CreateService(db, sender);

        var first = await service.SubscribeAsync("customer@example.com", true);
        var duplicate = await service.SubscribeAsync(" Customer@Example.com ", true);

        Assert.That(first, Is.EqualTo(NewsletterSignupOutcome.Subscribed));
        Assert.That(duplicate, Is.EqualTo(NewsletterSignupOutcome.AlreadySubscribed));
        Assert.That(await db.NewsletterSubscribers.CountAsync(), Is.EqualTo(1));
        Assert.That(sender.Calls, Is.EqualTo(1));
    }

    [TestCase(true, NewsletterSignupOutcome.Subscribed)]
    [TestCase(false, NewsletterSignupOutcome.SubscribedEmailFailed)]
    public async Task Saves_Subscription_And_Reports_Delivery(bool sent, NewsletterSignupOutcome expected)
    {
        await using var db = NewsletterSignupPreparationTests.CreateDatabase();
        var sender = new RecordingSender(sent);
        var service = CreateService(db, sender);

        var outcome = await service.SubscribeAsync(" Customer@Example.com ", true);

        Assert.That(outcome, Is.EqualTo(expected));
        var subscriber = await db.NewsletterSubscribers.SingleAsync();
        Assert.That(subscriber.IsSubscribed, Is.True);
        Assert.That(subscriber.ConsentText, Is.EqualTo(NewsletterSignupInput.ConsentText));
        Assert.That(sender.Calls, Is.EqualTo(1));
        Assert.That(sender.Url, Does.StartWith("https://example.com/Newsletter/Unsubscribe?token="));
    }

    internal static NewsletterSignupService CreateService(OrderDbContext db, RecordingSender sender) =>
        new(NewsletterSignupPreparationTests.CreatePreparation(db),
            new NewsletterSignupStore(db), new NewsletterSignupDelivery(sender));

    internal sealed class RecordingSender(bool sent) : INewsletterWelcomeSender
    {
        public int Calls { get; private set; }
        public string? Url { get; private set; }
        public Task<bool> SendWithResultAsync(string email, string? unsubscribeUrl, CancellationToken cancellationToken = default)
        {
            Calls++;
            Url = unsubscribeUrl;
            return Task.FromResult(sent);
        }
    }
}
