namespace Nordly.Web.Services;

public enum NewsletterSignupOutcome
{
    Subscribed,
    SubscribedEmailFailed,
    ConsentRequired,
    InvalidEmail,
    AlreadySubscribed,
    InvalidConfiguration
}

public sealed class NewsletterSignupService(
    NewsletterSignupPreparation preparation,
    NewsletterSignupStore store,
    NewsletterSignupDelivery delivery)
{
    public async Task<NewsletterSignupOutcome> SubscribeAsync(
        string? email, bool consent, CancellationToken cancellationToken = default)
    {
        var prepared = await preparation.PrepareAsync(email, consent, cancellationToken);
        if (prepared.Status != NewsletterSignupStatus.Ready)
            return prepared.Status switch
            {
                NewsletterSignupStatus.ConsentRequired => NewsletterSignupOutcome.ConsentRequired,
                NewsletterSignupStatus.InvalidEmail => NewsletterSignupOutcome.InvalidEmail,
                NewsletterSignupStatus.AlreadySubscribed => NewsletterSignupOutcome.AlreadySubscribed,
                NewsletterSignupStatus.InvalidConfiguration => NewsletterSignupOutcome.InvalidConfiguration,
                _ => throw new InvalidOperationException("Unexpected newsletter signup status.")
            };

        await store.SaveAsync(prepared, cancellationToken);
        return await delivery.SendAsync(prepared, cancellationToken)
            ? NewsletterSignupOutcome.Subscribed
            : NewsletterSignupOutcome.SubscribedEmailFailed;
    }
}
