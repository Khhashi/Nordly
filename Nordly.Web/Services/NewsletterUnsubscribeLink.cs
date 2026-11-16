namespace Nordly.Web.Services;

public sealed class NewsletterUnsubscribeLink(
    IConfiguration configuration,
    NewsletterUnsubscribeToken tokenService,
    ILogger<NewsletterUnsubscribeLink> logger)
{
    public bool TryCreate(Guid subscriberId, Guid version, out string url)
    {
        url = string.Empty;
        var configuredBaseUrl = configuration["PublicBaseUrl"];
        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrEmpty(baseUri.Host)
            || !string.IsNullOrEmpty(baseUri.UserInfo)
            || !string.IsNullOrEmpty(baseUri.Query)
            || !string.IsNullOrEmpty(baseUri.Fragment))
        {
            logger.LogError("PublicBaseUrl must be an absolute HTTPS URL without credentials, query, or fragment.");
            return false;
        }

        var token = tokenService.Create(subscriberId, version);
        url = $"{baseUri.AbsoluteUri.TrimEnd('/')}/Newsletter/Unsubscribe?token={Uri.EscapeDataString(token)}";
        return true;
    }
}
