using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Domain.Entities;
using Nordly.Infrastructure.Data;
using Nordly.Web.Pages;
using Nordly.Web.Pages.Newsletter;
using Nordly.Web.Services;
using Npgsql;

namespace Nordly.Tests;

public class NewsletterSubscriptionTests
{
    private static readonly NewsletterUnsubscribeToken TokenService = new(new EphemeralDataProtectionProvider());

    [Test]
    public async Task New_Subscription_Normalizes_Email_And_Sends_One_Welcome_Email()
    {
        await using var db = CreateDatabase();
        var welcomeSender = new RecordingWelcomeEmailSender();
        var model = CreateModel(db, welcomeSender, out var tempData);

        var result = await model.OnPostSubscribeNewsletter("  Customer@Example.com ", consent: true);

        Assert.That(result, Is.TypeOf<RedirectToPageResult>());
        var subscriber = await db.NewsletterSubscribers.SingleAsync();
        Assert.That(subscriber.Email, Is.EqualTo("customer@example.com"));
        Assert.That(subscriber.IsSubscribed, Is.True);
        Assert.That(subscriber.ConsentGivenAt, Is.Not.Null);
        Assert.That(subscriber.ConsentText, Is.EqualTo(model.NewsletterConsentText));
        Assert.That(subscriber.UnsubscribeVersion, Is.Not.Null);
        Assert.That(welcomeSender.SentEmails, Is.EqualTo(new[] { "customer@example.com" }));
        Assert.That(welcomeSender.UnsubscribeUrls.Single(), Does.StartWith("https://nordly.example/Newsletter/Unsubscribe?token="));
        var link = new Uri(welcomeSender.UnsubscribeUrls.Single());
        var token = QueryHelpers.ParseQuery(link.Query)["token"].ToString();
        var unsubscribe = CreateUnsubscribeModel(db);
        await unsubscribe.OnGetAsync(token);
        Assert.That(unsubscribe.CanUnsubscribe, Is.True);
        await unsubscribe.OnPostAsync();
        Assert.That(subscriber.IsSubscribed, Is.False);
        Assert.That(tempData["NewsletterSuccess"], Is.EqualTo("Du er nå påmeldt vårt nyhetsbrev."));
        Assert.That(tempData.ContainsKey("NewsletterError"), Is.False);
    }

    [Test]
    public async Task Subscription_Without_Consent_Is_Rejected()
    {
        await using var db = CreateDatabase();
        var welcomeSender = new RecordingWelcomeEmailSender();
        var model = CreateModel(db, welcomeSender, out var tempData);

        await model.OnPostSubscribeNewsletter("customer@example.com", consent: false);

        Assert.That(await db.NewsletterSubscribers.CountAsync(), Is.Zero);
        Assert.That(welcomeSender.SentEmails, Is.Empty);
        Assert.That(tempData["NewsletterError"], Is.EqualTo("Du må samtykke før du kan melde deg på."));
    }

    [Test]
    public async Task Existing_Email_Is_Reported_As_Duplicate_Without_Sending_Welcome_Email()
    {
        await using var db = CreateDatabase();
        db.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = "customer@example.com", IsSubscribed = true });
        await db.SaveChangesAsync();
        var welcomeSender = new RecordingWelcomeEmailSender();
        var model = CreateModel(db, welcomeSender, out var tempData);

        await model.OnPostSubscribeNewsletter(" Customer@Example.com ", consent: true);

        Assert.That(await db.NewsletterSubscribers.CountAsync(), Is.EqualTo(1));
        Assert.That(welcomeSender.SentEmails, Is.Empty);
        Assert.That(tempData["NewsletterError"], Is.EqualTo("Denne e-posten er allerede registrert."));
    }

    [Test]
    public async Task Concurrent_Unique_Email_Conflict_Is_Reported_As_Duplicate()
    {
        var databaseError = new DbUpdateException(
            "Unique constraint violation.",
            new PostgresException(
                "duplicate key value violates unique constraint",
                "ERROR",
                "ERROR",
                PostgresErrorCodes.UniqueViolation,
                constraintName: "IX_NewsletterSubscribers_Email"));
        await using var db = CreateDatabase(new FailingSaveChangesInterceptor(databaseError));
        var welcomeSender = new RecordingWelcomeEmailSender();
        var model = CreateModel(db, welcomeSender, out var tempData);

        await model.OnPostSubscribeNewsletter("customer@example.com", consent: true);

        Assert.That(welcomeSender.SentEmails, Is.Empty);
        Assert.That(tempData["NewsletterError"], Is.EqualTo("Denne e-posten er allerede registrert."));
    }

    [Test]
    public async Task Unexpected_Save_Failure_Shows_Retry_Message_And_Does_Not_Send_Email()
    {
        await using var db = CreateDatabase(new FailingSaveChangesInterceptor(new DbUpdateException("Database unavailable.")));
        var welcomeSender = new RecordingWelcomeEmailSender();
        var model = CreateModel(db, welcomeSender, out var tempData);

        await model.OnPostSubscribeNewsletter("customer@example.com", consent: true);

        Assert.That(welcomeSender.SentEmails, Is.Empty);
        Assert.That(tempData["NewsletterError"], Is.EqualTo("Påmeldingen kunne ikke fullføres akkurat nå. Prøv igjen senere."));
        Assert.That(tempData.ContainsKey("NewsletterSuccess"), Is.False);
    }

    [Test]
    public async Task Unsubscribed_Email_Can_Subscribe_Again_With_Fresh_Consent_And_Token()
    {
        await using var db = CreateDatabase();
        var oldSubscriber = new NewsletterSubscriber
        {
            Email = "customer@example.com",
            IsSubscribed = true,
            UnsubscribeVersion = Guid.NewGuid()
        };
        var oldToken = TokenService.Create(oldSubscriber.Id, oldSubscriber.UnsubscribeVersion.Value);
        db.NewsletterSubscribers.Add(oldSubscriber);
        await db.SaveChangesAsync();

        var unsubscribeModel = CreateUnsubscribeModel(db);
        await unsubscribeModel.OnGetAsync(oldToken);
        await unsubscribeModel.OnPostAsync();

        var welcomeSender = new RecordingWelcomeEmailSender();
        var model = CreateModel(db, welcomeSender, out var tempData);

        await model.OnPostSubscribeNewsletter("customer@example.com", consent: true);

        var subscriber = await db.NewsletterSubscribers.SingleAsync();
        Assert.That(await db.NewsletterSubscribers.CountAsync(), Is.EqualTo(1));
        Assert.That(subscriber.IsSubscribed, Is.True);
        Assert.That(subscriber.UnsubscribedAt, Is.Null);
        Assert.That(subscriber.ConsentGivenAt, Is.Not.Null);
        Assert.That(subscriber.ConsentText, Is.EqualTo(model.NewsletterConsentText));
        Assert.That(subscriber.UnsubscribeVersion, Is.Not.Null);
        Assert.That(welcomeSender.SentEmails, Is.EqualTo(new[] { "customer@example.com" }));
        Assert.That(tempData["NewsletterSuccess"], Is.EqualTo("Du er nå påmeldt vårt nyhetsbrev."));

        var staleLinkModel = CreateUnsubscribeModel(db);
        await staleLinkModel.OnGetAsync(oldToken);
        Assert.That(staleLinkModel.CanUnsubscribe, Is.False);
    }

    [Test]
    public async Task Failed_Welcome_Email_Leaves_Subscription_Inactive_And_Allows_Retry()
    {
        await using var db = CreateDatabase();
        var welcomeSender = new RecordingWelcomeEmailSender { Succeeds = false };
        var model = CreateModel(db, welcomeSender, out var tempData);

        await model.OnPostSubscribeNewsletter("customer@example.com", consent: true);

        var subscriber = await db.NewsletterSubscribers.SingleAsync();
        Assert.That(subscriber.IsSubscribed, Is.False);
        Assert.That(subscriber.UnsubscribeVersion, Is.Null);
        Assert.That(tempData["NewsletterError"] as string, Does.Contain("e-posten ikke ble sendt"));

        welcomeSender.Succeeds = true;
        var retry = CreateModel(db, welcomeSender, out var retryTempData);
        await retry.OnPostSubscribeNewsletter("customer@example.com", consent: true);
        Assert.That(subscriber.IsSubscribed, Is.True);
        Assert.That(retryTempData["NewsletterSuccess"], Is.Not.Null);
    }

    private static OrderDbContext CreateDatabase(ISaveChangesInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor is null ? Array.Empty<IInterceptor>() : new IInterceptor[] { interceptor })
            .Options;
        return new OrderDbContext(options);
    }

    private static IndexModel CreateModel(OrderDbContext db, RecordingWelcomeEmailSender welcomeSender, out TempDataDictionary tempData)
    {
        var httpContext = new DefaultHttpContext();
        tempData = new TempDataDictionary(httpContext, new FakeTempDataProvider());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PublicBaseUrl"] = "https://nordly.example" })
            .Build();
        return new IndexModel(db, welcomeSender, NullLogger<IndexModel>.Instance, configuration, TokenService)
        {
            PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext { HttpContext = httpContext },
            TempData = tempData
        };
    }

    private static UnsubscribeModel CreateUnsubscribeModel(OrderDbContext db)
    {
        var httpContext = new DefaultHttpContext();
        return new UnsubscribeModel(db, TokenService)
        {
            PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext { HttpContext = httpContext }
        };
    }

    private sealed class RecordingWelcomeEmailSender : IWelcomeEmailSender
    {
        public List<string> SentEmails { get; } = new();
        public List<string> UnsubscribeUrls { get; } = new();
        public bool Succeeds { get; set; } = true;

        public Task<bool> SendAsync(string email, string unsubscribeUrl, CancellationToken cancellationToken = default)
        {
            SentEmails.Add(email);
            UnsubscribeUrls.Add(unsubscribeUrl);
            return Task.FromResult(Succeeds);
        }
    }

    private sealed class FailingSaveChangesInterceptor(DbUpdateException exception) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) => throw exception;
    }

    private sealed class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
