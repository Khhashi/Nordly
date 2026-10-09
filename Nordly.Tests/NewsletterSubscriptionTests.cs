using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Domain.Entities;
using Nordly.Infrastructure.Data;
using Nordly.Web.Pages;
using Nordly.Web.Services;
using Npgsql;

namespace Nordly.Tests;

public class NewsletterSubscriptionTests
{
    [Test]
    public async Task New_Subscription_Normalizes_Email_And_Sends_One_Welcome_Email()
    {
        await using var db = CreateDatabase();
        var welcomeSender = new RecordingWelcomeEmailSender();
        var model = CreateModel(db, welcomeSender, out var tempData);

        var result = await model.OnPostSubscribeNewsletter("  Customer@Example.com ");

        Assert.That(result, Is.TypeOf<RedirectToPageResult>());
        Assert.That(await db.NewsletterSubscribers.Select(item => item.Email).SingleAsync(), Is.EqualTo("customer@example.com"));
        Assert.That(welcomeSender.SentEmails, Is.EqualTo(new[] { "customer@example.com" }));
        Assert.That(tempData["NewsletterSuccess"], Is.EqualTo("Du er nå påmeldt vårt nyhetsbrev."));
        Assert.That(tempData.ContainsKey("NewsletterError"), Is.False);
    }

    [Test]
    public async Task Existing_Email_Is_Reported_As_Duplicate_Without_Sending_Welcome_Email()
    {
        await using var db = CreateDatabase();
        db.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = "customer@example.com" });
        await db.SaveChangesAsync();
        var welcomeSender = new RecordingWelcomeEmailSender();
        var model = CreateModel(db, welcomeSender, out var tempData);

        await model.OnPostSubscribeNewsletter(" Customer@Example.com ");

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

        await model.OnPostSubscribeNewsletter("customer@example.com");

        Assert.That(welcomeSender.SentEmails, Is.Empty);
        Assert.That(tempData["NewsletterError"], Is.EqualTo("Denne e-posten er allerede registrert."));
    }

    [Test]
    public async Task Unexpected_Save_Failure_Shows_Retry_Message_And_Does_Not_Send_Email()
    {
        await using var db = CreateDatabase(new FailingSaveChangesInterceptor(new DbUpdateException("Database unavailable.")));
        var welcomeSender = new RecordingWelcomeEmailSender();
        var model = CreateModel(db, welcomeSender, out var tempData);

        await model.OnPostSubscribeNewsletter("customer@example.com");

        Assert.That(welcomeSender.SentEmails, Is.Empty);
        Assert.That(tempData["NewsletterError"], Is.EqualTo("Påmeldingen kunne ikke fullføres akkurat nå. Prøv igjen senere."));
        Assert.That(tempData.ContainsKey("NewsletterSuccess"), Is.False);
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
        return new IndexModel(db, welcomeSender, NullLogger<IndexModel>.Instance)
        {
            PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext { HttpContext = httpContext },
            TempData = tempData
        };
    }

    private sealed class RecordingWelcomeEmailSender : IWelcomeEmailSender
    {
        public List<string> SentEmails { get; } = new();

        public Task SendAsync(string email, CancellationToken cancellationToken = default)
        {
            SentEmails.Add(email);
            return Task.CompletedTask;
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
