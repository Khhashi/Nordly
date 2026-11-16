using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Nordly.Web.Services;

public sealed class NewsletterSignupRequest(
    NewsletterSignupService service,
    ILogger<NewsletterSignupRequest> logger)
{
    public async Task<NewsletterSignupMessage> HandleAsync(
        string? email, bool consent, CancellationToken cancellationToken = default)
    {
        try
        {
            var outcome = await service.SubscribeAsync(email, consent, cancellationToken);
            return NewsletterSignupMessage.FromOutcome(outcome);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation("Newsletter subscription changed concurrently.");
            return RetryMessage();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError("Could not save newsletter subscription. Database error: {ErrorType}.",
                exception.InnerException?.GetType().Name ?? exception.GetType().Name);
            return RetryMessage();
        }
        catch (NpgsqlException exception)
        {
            logger.LogError("Could not access newsletter subscription database: {ErrorType}.",
                exception.GetType().Name);
            return RetryMessage();
        }
    }

    private static NewsletterSignupMessage RetryMessage() =>
        new("NewsletterError", "Påmeldingen kunne ikke fullføres akkurat nå. Prøv igjen senere.");
}
