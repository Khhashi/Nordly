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
    private readonly OrderDbContext _db;
    private readonly IWelcomeEmailSender _welcomeEmailSender;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(OrderDbContext db, IWelcomeEmailSender welcomeEmailSender, ILogger<IndexModel> logger)
    {
        _db = db;
        _welcomeEmailSender = welcomeEmailSender;
        _logger = logger;
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

    public async Task<IActionResult> OnPostSubscribeNewsletter(string email)
    {
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
        catch
        {
            TempData["NewsletterError"] = "Skriv inn en gyldig e-postadresse.";
            return RedirectToPage(null, null, "newsletter");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await _db.NewsletterSubscribers.AnyAsync(subscriber => subscriber.Email == normalizedEmail))
        {
            TempData["NewsletterError"] = "Denne e-posten er allerede registrert.";
            return RedirectToPage(null, null, "newsletter");
        }

        _db.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = normalizedEmail });
        try
        {
            await _db.SaveChangesAsync();
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

        // Kommer bare hit første gang adressen meldes på, så velkomst-e-posten sendes én gang.
        await _welcomeEmailSender.SendAsync(normalizedEmail);

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
