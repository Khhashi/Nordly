using Microsoft.EntityFrameworkCore;
using Nordly.Domain.Entities;
using Nordly.Infrastructure.Data;

namespace Nordly.Web.Services;

public enum NewsletterSignupStatus
{
    Ready,
    ConsentRequired,
    InvalidEmail,
    AlreadySubscribed,
    InvalidConfiguration
}

public sealed record PreparedNewsletterSignup(
    NewsletterSignupStatus Status,
    NewsletterSubscriber? Subscriber = null,
    Guid? PreviousVersion = null,
    bool IsNew = false,
    string? UnsubscribeUrl = null);

public sealed class NewsletterSignupPreparation(OrderDbContext db, NewsletterUnsubscribeLink links)
{
    public async Task<PreparedNewsletterSignup> PrepareAsync(
        string? email, bool consent, CancellationToken cancellationToken = default)
    {
        if (!consent)
            return new(NewsletterSignupStatus.ConsentRequired);
        if (!NewsletterSignupInput.TryNormalizeEmail(email, out var normalizedEmail))
            return new(NewsletterSignupStatus.InvalidEmail);

        var subscriber = await db.NewsletterSubscribers.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Email == normalizedEmail, cancellationToken);
        if (subscriber?.IsSubscribed == true)
            return new(NewsletterSignupStatus.AlreadySubscribed);

        var isNew = subscriber is null;
        subscriber ??= new NewsletterSubscriber { Email = normalizedEmail };
        var previousVersion = subscriber.UnsubscribeVersion;
        NewsletterSubscriptionState.TryActivate(subscriber);
        if (!links.TryCreate(subscriber.Id, subscriber.UnsubscribeVersion!.Value, out var url))
            return new(NewsletterSignupStatus.InvalidConfiguration);

        return new(NewsletterSignupStatus.Ready, subscriber, previousVersion, isNew, url);
    }
}
