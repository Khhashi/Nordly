using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nordly.Domain.Entities;
using Nordly.Infrastructure.Data;
using Nordly.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace Nordly.Web.Pages;

public class IndexModel : PageModel
{
    public string NewsletterConsentText =>
        "Ja, jeg samtykker til at NORDLY sender meg nyhetsbrev med nyheter og tilbud. Jeg kan melde meg av når som helst.";

    private readonly OrderDbContext _db;
    private readonly IWelcomeEmailSender _welcomeEmailSender;
    private readonly ILogger<IndexModel> _logger;
    private readonly IConfiguration _configuration;
    private readonly NewsletterUnsubscribeToken _unsubscribeToken;

    public IndexModel(
        OrderDbContext db,
        IWelcomeEmailSender welcomeEmailSender,
        ILogger<IndexModel> logger,
        IConfiguration configuration,
        NewsletterUnsubscribeToken unsubscribeToken)
    {
        _db = db;
        _welcomeEmailSender = welcomeEmailSender;
        _logger = logger;
        _configuration = configuration;
        _unsubscribeToken = unsubscribeToken;
    }

    public IReadOnlyList<StoreProduct> Products => StorefrontCatalog.Products;
    public int CartCount { get; private set; }
    public IReadOnlyDictionary<Guid, int> ProductQuantities { get; private set; } = new Dictionary<Guid, int>();

    public void OnGet()
    {
        var cart = Cart.Load(HttpContext.Session);
        CartCount = cart.Count;
        ProductQuantities = cart.Items.ToDictionary(item => item.ProductId, item => item.Quantity);
    }

    public IActionResult OnPostAddToCart(Guid productId)
    {
        if (!StorefrontCatalog.Products.Any(product => product.Id == productId))
        {
            return NotFound();
        }

        var cart = Cart.Load(HttpContext.Session);
        cart.Add(productId);
        cart.Save(HttpContext.Session);

        if (IsAjaxRequest())
            return new JsonResult(new { success = true, quantity = cart.QuantityOf(productId), cartCount = cart.Count });

        TempData["Success"] = "Produktet er lagt i handlekurven";
        return RedirectToPage();
    }

    public IActionResult OnPostRemoveFromCart(Guid productId)
    {
        var cart = Cart.Load(HttpContext.Session);
        cart.Decrease(productId);
        cart.Save(HttpContext.Session);

        if (IsAjaxRequest())
            return new JsonResult(new { success = true, quantity = cart.QuantityOf(productId), cartCount = cart.Count });

        TempData["Success"] = "Antallet er oppdatert.";
        return RedirectToPage();
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IActionResult> OnPostSubscribeNewsletter(string email, bool consent)
    {
        if (!consent)
        {
            TempData["NewsletterError"] = "Du må samtykke før du kan melde deg på.";
            return RedirectToPage(null, null, "newsletter");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["NewsletterError"] = "Skriv inn en gyldig e-postadresse.";
            return RedirectToPage(null, null, "newsletter");
        }

        try
        {
            var address = new MailAddress(email);
            if (string.IsNullOrWhiteSpace(address.Address))
            {
                TempData["NewsletterError"] = "Skriv inn en gyldig e-postadresse.";
                return RedirectToPage(null, null, "newsletter");
            }
        }
        catch (FormatException)
        {
            TempData["NewsletterError"] = "Skriv inn en gyldig e-postadresse.";
            return RedirectToPage(null, null, "newsletter");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var subscriber = await _db.NewsletterSubscribers
            .SingleOrDefaultAsync(item => item.Email == normalizedEmail);
        if (subscriber is { IsSubscribed: true })
        {
            TempData["NewsletterError"] = "Denne e-posten er allerede registrert.";
            return RedirectToPage(null, null, "newsletter");
        }

        var publicBaseUrl = _configuration["PublicBaseUrl"];
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme != Uri.UriSchemeHttps)
        {
            _logger.LogError("Newsletter subscription rejected because PublicBaseUrl is missing or is not an absolute HTTPS URL.");
            TempData["NewsletterError"] = "Påmeldingen kunne ikke fullføres akkurat nå. Prøv igjen senere.";
            return RedirectToPage(null, null, "newsletter");
        }

        var consentAt = DateTime.UtcNow;
        if (subscriber is null)
        {
            subscriber = new NewsletterSubscriber { Email = normalizedEmail };
            _db.NewsletterSubscribers.Add(subscriber);
        }

        subscriber.SubscribedAt = consentAt;
        subscriber.ConsentGivenAt = consentAt;
        subscriber.ConsentText = NewsletterConsentText;
        subscriber.IsSubscribed = true;
        subscriber.UnsubscribedAt = null;
        subscriber.UnsubscribeVersion = Guid.NewGuid();

        var token = _unsubscribeToken.Create(subscriber.Id, subscriber.UnsubscribeVersion.Value);
        var unsubscribeUrl = new UriBuilder(baseUri)
        {
            Path = "/Newsletter/Unsubscribe",
            Query = $"token={Uri.EscapeDataString(token)}"
        }.Uri.ToString();

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["NewsletterError"] = "Påmeldingen ble endret samtidig. Prøv igjen.";
            return RedirectToPage(null, null, "newsletter");
        }
        catch (DbUpdateException exception) when (IsDuplicateEmailError(exception))
        {
            TempData["NewsletterError"] = "Denne e-posten er allerede registrert.";
            return RedirectToPage(null, null, "newsletter");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not save newsletter subscription.");
            TempData["NewsletterError"] = "Påmeldingen kunne ikke fullføres akkurat nå. Prøv igjen senere.";
            return RedirectToPage(null, null, "newsletter");
        }

        if (!await _welcomeEmailSender.SendAsync(normalizedEmail, unsubscribeUrl))
        {
            subscriber.IsSubscribed = false;
            subscriber.UnsubscribeVersion = null;
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(exception, "Could not deactivate newsletter subscription after email delivery failed.");
                TempData["NewsletterError"] = "E-posten kunne ikke sendes. Kontakt kundeservice for hjelp med påmeldingen.";
                return RedirectToPage(null, null, "newsletter");
            }

            TempData["NewsletterError"] = "Påmeldingen kunne ikke fullføres fordi e-posten ikke ble sendt. Prøv igjen senere.";
            return RedirectToPage(null, null, "newsletter");
        }

        TempData["NewsletterSuccess"] = "Du er nå påmeldt vårt nyhetsbrev.";
        return RedirectToPage(null, null, "newsletter");
    }

    private static bool IsDuplicateEmailError(DbUpdateException exception)
    {
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
        {
            if (cause is Npgsql.PostgresException postgresException
                && postgresException.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation
                && postgresException.ConstraintName == "IX_NewsletterSubscribers_Email")
            {
                return true;
            }
        }

        return false;
    }
}
