using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nordly.Domain.Entities;
using Nordly.Infrastructure.Data;
using Nordly.Web.Services;

namespace Nordly.Web.Pages.Newsletter;

public partial class UnsubscribeModel(
    OrderDbContext db,
    NewsletterUnsubscribeToken tokenService,
    ILogger<UnsubscribeModel> logger) : PageModel
{
    [BindProperty]
    public string? Token { get; set; }

    public bool CanUnsubscribe { get; private set; }
    public bool IsUnsubscribed { get; private set; }

    public async Task OnGetAsync(string? token)
    {
        SetPrivacyHeaders();
        Token = token;
        CanUnsubscribe = await FindSubscribedSubscriberAsync(token) is not null;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetPrivacyHeaders();
        var subscriber = await FindSubscribedSubscriberAsync(Token);
        if (subscriber is null)
        {
            CanUnsubscribe = false;
            return Page();
        }

        subscriber.IsSubscribed = false;
        subscriber.UnsubscribedAt = DateTime.UtcNow;
        subscriber.UnsubscribeVersion = null;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation("Newsletter subscription changed before unsubscribe could be confirmed.");
            CanUnsubscribe = false;
            return Page();
        }

        CanUnsubscribe = false;
        IsUnsubscribed = true;
        return Page();
    }

    private async Task<NewsletterSubscriber?> FindSubscribedSubscriberAsync(string? token)
    {
        if (!tokenService.TryRead(token, out var subscriberId, out var version))
            return null;

        return await db.NewsletterSubscribers.SingleOrDefaultAsync(subscriber =>
            subscriber.Id == subscriberId && subscriber.IsSubscribed && subscriber.UnsubscribeVersion == version);
    }

    private void SetPrivacyHeaders()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    }
}
