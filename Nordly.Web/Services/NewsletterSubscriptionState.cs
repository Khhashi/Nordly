using Nordly.Domain.Entities;

namespace Nordly.Web.Services;

public static class NewsletterSubscriptionState
{
    public static bool TryActivate(NewsletterSubscriber subscriber)
    {
        if (subscriber.IsSubscribed)
            return false;

        var now = DateTime.UtcNow;
        subscriber.SubscribedAt = now;
        subscriber.ConsentGivenAt = now;
        subscriber.ConsentText = NewsletterSignupInput.ConsentText;
        subscriber.IsSubscribed = true;
        subscriber.UnsubscribedAt = null;
        subscriber.UnsubscribeVersion = Guid.NewGuid();
        return true;
    }
}
