using Nordly.Infrastructure.Data;

namespace Nordly.Web.Services;

public sealed class NewsletterSignupStore(OrderDbContext db)
{
    public async Task SaveAsync(PreparedNewsletterSignup prepared, CancellationToken cancellationToken = default)
    {
        if (prepared.Status != NewsletterSignupStatus.Ready || prepared.Subscriber is null)
            throw new ArgumentException("Only a prepared signup can be saved.", nameof(prepared));

        if (prepared.IsNew)
        {
            db.NewsletterSubscribers.Add(prepared.Subscriber);
        }
        else
        {
            var entry = db.NewsletterSubscribers.Update(prepared.Subscriber);
            entry.Property(subscriber => subscriber.UnsubscribeVersion).OriginalValue = prepared.PreviousVersion;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
