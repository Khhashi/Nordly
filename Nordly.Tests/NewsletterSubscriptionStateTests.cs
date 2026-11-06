using Nordly.Domain.Entities;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterSubscriptionStateTests
{
    [Test]
    public void Activation_Records_Consent_And_New_Version()
    {
        var subscriber = new NewsletterSubscriber { UnsubscribedAt = DateTime.UtcNow };
        Assert.That(NewsletterSubscriptionState.TryActivate(subscriber), Is.True);
        Assert.That(subscriber.IsSubscribed, Is.True);
        Assert.That(subscriber.ConsentGivenAt, Is.EqualTo(subscriber.SubscribedAt));
        Assert.That(subscriber.ConsentText, Is.EqualTo(NewsletterSignupInput.ConsentText));
        Assert.That(subscriber.UnsubscribedAt, Is.Null);
        Assert.That(subscriber.UnsubscribeVersion, Is.Not.Null.And.Not.EqualTo(Guid.Empty));
    }
}
