using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class BrevoWelcomeEmailSenderTests
{
    [Test]
    public async Task Missing_Mail_Configuration_Is_Reported()
    {
        INewsletterWelcomeSender sender = new NoWelcomeEmailSender(NullLogger<NoWelcomeEmailSender>.Instance);
        Assert.That(await sender.SendWithResultAsync("kunde@example.com", "https://nordly.example/unsubscribe"), Is.False);
    }

    [Test]
    public async Task Missing_Brevo_Key_Is_Reported()
    {
        var handler = new CapturingHandler();
        INewsletterWelcomeSender sender = CreateSender(handler, apiKey: null);
        Assert.That(await sender.SendWithResultAsync("kunde@example.com", "https://nordly.example/unsubscribe"), Is.False);
        Assert.That(handler.Request, Is.Null);
    }

    [Test]
    public async Task SendWithResultAsync_Includes_Unsubscribe_Link()
    {
        var handler = new CapturingHandler();
        var sender = CreateSender(handler);

        var sent = await sender.SendWithResultAsync("kunde@example.com", "https://nordly.example/unsubscribe");

        Assert.That(sent, Is.True);
        Assert.That(handler.Body, Does.Contain("https://nordly.example/unsubscribe"));
    }

    [Test]
    public async Task SendWithResultAsync_Reports_Rejection()
    {
        var handler = new CapturingHandler { StatusCode = HttpStatusCode.BadRequest };
        var sender = CreateSender(handler);

        Assert.That(await sender.SendWithResultAsync("kunde@example.com", "https://nordly.example/unsubscribe"), Is.False);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Created;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(StatusCode) { Content = new StringContent("{}") };
        }
    }

    private static BrevoWelcomeEmailSender CreateSender(CapturingHandler handler, string? apiKey = "test-key")
    {
        var settings = new Dictionary<string, string?>
        {
            ["Email:BrevoApiKey"] = apiKey,
            ["Email:FromAddress"] = "butikk@example.com",
            ["Email:FromName"] = "Nordly"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new BrevoWelcomeEmailSender(
            new HttpClient(handler), configuration, NullLogger<BrevoWelcomeEmailSender>.Instance);
    }

    [Test]
    public async Task SendAsync_PostsWelcomeEmailToSubscriber()
    {
        var handler = new CapturingHandler();
        var sender = CreateSender(handler);

        await sender.SendAsync("kunde@example.com");

        Assert.That(handler.Request, Is.Not.Null);
        Assert.That(handler.Request!.RequestUri!.ToString(), Is.EqualTo(BrevoOrderConfirmationEmailSender.Endpoint));
        Assert.That(handler.Request.Headers.GetValues("api-key").Single(), Is.EqualTo("test-key"));
        Assert.That(handler.Body, Does.Contain("kunde@example.com"));
        Assert.That(handler.Body, Does.Contain("Velkommen til Nordly"));
    }

    [Test]
    public async Task SendAsync_SkipsWhenApiKeyIsMissing()
    {
        var handler = new CapturingHandler();
        var sender = CreateSender(handler, apiKey: null);

        await sender.SendAsync("kunde@example.com");

        Assert.That(handler.Request, Is.Null);
    }

    [Test]
    public void SendAsync_DoesNotThrowWhenBrevoRejects()
    {
        var handler = new CapturingHandler { StatusCode = HttpStatusCode.BadRequest };
        var sender = CreateSender(handler);

        Assert.DoesNotThrowAsync(() => sender.SendAsync("kunde@example.com"));
    }
}
