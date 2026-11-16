using Nordly.Domain.Entities;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterSignupDeliveryTests
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task Reports_The_Actual_Delivery_Result(bool deliveryResult)
    {
        var sender = new RecordingSender(deliveryResult);
        var prepared = new PreparedNewsletterSignup(NewsletterSignupStatus.Ready,
            new NewsletterSubscriber { Email = "customer@example.com" },
            null, true, "https://example.com/unsubscribe");

        var result = await new NewsletterSignupDelivery(sender).SendAsync(prepared);

        Assert.That(result, Is.EqualTo(deliveryResult));
        Assert.That(sender.Email, Is.EqualTo("customer@example.com"));
        Assert.That(sender.Url, Is.EqualTo("https://example.com/unsubscribe"));
    }

    private sealed class RecordingSender(bool result) : INewsletterWelcomeSender
    {
        public string? Email { get; private set; }
        public string? Url { get; private set; }
        public Task<bool> SendWithResultAsync(string email, string? unsubscribeUrl, CancellationToken cancellationToken = default)
        {
            Email = email;
            Url = unsubscribeUrl;
            return Task.FromResult(result);
        }
    }
}
