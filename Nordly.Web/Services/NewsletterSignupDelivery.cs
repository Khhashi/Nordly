namespace Nordly.Web.Services;

public sealed class NewsletterSignupDelivery(IWelcomeEmailSender sender)
{
    public async Task<bool> SendAsync(
        PreparedNewsletterSignup prepared, CancellationToken cancellationToken = default)
    {
        if (prepared.Status != NewsletterSignupStatus.Ready
            || prepared.Subscriber is null || string.IsNullOrEmpty(prepared.UnsubscribeUrl))
            throw new ArgumentException("Only a prepared signup with a link can be sent.", nameof(prepared));

        return await sender.SendAsync(
            prepared.Subscriber.Email, prepared.UnsubscribeUrl, cancellationToken);
    }
}
