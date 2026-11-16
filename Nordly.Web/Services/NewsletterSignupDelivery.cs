namespace Nordly.Web.Services;

public sealed class NewsletterSignupDelivery(INewsletterWelcomeSender sender)
{
    public async Task<bool> SendAsync(
        PreparedNewsletterSignup prepared, CancellationToken cancellationToken = default)
    {
        if (prepared.Status != NewsletterSignupStatus.Ready
            || prepared.Subscriber is null || string.IsNullOrEmpty(prepared.UnsubscribeUrl))
            throw new ArgumentException("Only a prepared signup with a link can be sent.", nameof(prepared));

        return await sender.SendWithResultAsync(
            prepared.Subscriber.Email, prepared.UnsubscribeUrl, cancellationToken);
    }
}
