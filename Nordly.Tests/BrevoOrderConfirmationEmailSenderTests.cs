using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Domain.Entities;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class BrevoOrderConfirmationEmailSenderTests
{
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

    private static BrevoOrderConfirmationEmailSender CreateSender(
        CapturingHandler handler, string? apiKey = "test-key")
    {
        var settings = new Dictionary<string, string?>
        {
            ["Email:BrevoApiKey"] = apiKey,
            ["Email:FromAddress"] = "butikk@example.com",
            ["Email:FromName"] = "Nordly"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new BrevoOrderConfirmationEmailSender(
            new HttpClient(handler), configuration, NullLogger<BrevoOrderConfirmationEmailSender>.Instance);
    }

    private static Order CreateOrder()
    {
        var order = new Order { CustomerEmail = "kunde@example.com", CustomerName = "Kari" };
        order.AddProduct(new Product("Ullgenser", 899m), 1);
        return order;
    }

    [Test]
    public async Task SendAsync_PostsToBrevoWithApiKeyAndRecipient()
    {
        var handler = new CapturingHandler();
        var sender = CreateSender(handler);

        await sender.SendAsync(CreateOrder());

        Assert.That(handler.Request, Is.Not.Null);
        Assert.That(handler.Request!.RequestUri!.ToString(), Is.EqualTo(BrevoOrderConfirmationEmailSender.Endpoint));
        Assert.That(handler.Request.Headers.GetValues("api-key").Single(), Is.EqualTo("test-key"));
        Assert.That(handler.Body, Does.Contain("kunde@example.com"));
        Assert.That(handler.Body, Does.Contain("Ordrebekreftelse"));
    }

    [Test]
    public async Task SendAsync_UsesShortOrderNumberInsteadOfFullId()
    {
        var handler = new CapturingHandler();
        var sender = CreateSender(handler);
        var order = CreateOrder();

        await sender.SendAsync(order);

        Assert.That(handler.Body, Does.Contain(order.Number));
        Assert.That(handler.Body, Does.Not.Contain(order.Id.ToString()));
    }

    [Test]
    public async Task SendAsync_SkipsWhenApiKeyIsMissing()
    {
        var handler = new CapturingHandler();
        var sender = CreateSender(handler, apiKey: null);

        await sender.SendAsync(CreateOrder());

        Assert.That(handler.Request, Is.Null);
    }

    [Test]
    public void SendAsync_DoesNotThrowWhenBrevoRejects()
    {
        var handler = new CapturingHandler { StatusCode = HttpStatusCode.BadRequest };
        var sender = CreateSender(handler);

        Assert.DoesNotThrowAsync(() => sender.SendAsync(CreateOrder()));
    }
}
