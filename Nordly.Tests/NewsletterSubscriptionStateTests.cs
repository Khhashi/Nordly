using Nordly.Domain.Entities;
using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterSubscriptionStateTests
{
    [Test]
    public void Reactivation_Replaces_Previous_Consent_And_Version()
    {
        var oldVersion = Guid.NewGuid();
        var oldDate = DateTime.UtcNow.AddDays(-1);
        var subscriber = new NewsletterSubscriber
        {
            ConsentGivenAt = oldDate,
            ConsentText = "Tidligere samtykke",
            UnsubscribedAt = oldDate,
            UnsubscribeVersion = oldVersion
        };

        Assert.That(NewsletterSubscriptionState.TryActivate(subscriber), Is.True);
        Assert.That(subscriber.UnsubscribeVersion, Is.Not.EqualTo(oldVersion));
        Assert.That(subscriber.ConsentGivenAt, Is.GreaterThan(oldDate));
        Assert.That(subscriber.ConsentText, Is.EqualTo(NewsletterSignupInput.ConsentText));
        Assert.That(subscriber.UnsubscribedAt, Is.Null);
    }

    [Test]
    public void Already_Active_Subscription_Is_Not_Modified()
    {
        var version = Guid.NewGuid();
        var date = DateTime.UtcNow.AddDays(-1);
        var subscriber = new NewsletterSubscriber
        {
            IsSubscribed = true,
            UnsubscribeVersion = version,
            ConsentGivenAt = date,
            ConsentText = "Eksisterende samtykke",
            SubscribedAt = date
        };

        Assert.That(NewsletterSubscriptionState.TryActivate(subscriber), Is.False);
        Assert.That(subscriber.UnsubscribeVersion, Is.EqualTo(version));
        Assert.That(subscriber.ConsentGivenAt, Is.EqualTo(date));
        Assert.That(subscriber.SubscribedAt, Is.EqualTo(date));
        Assert.That(subscriber.ConsentText, Is.EqualTo("Eksisterende samtykke"));
    }

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
