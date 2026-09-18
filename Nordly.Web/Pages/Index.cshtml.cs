using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using PG3302.Domain.Entities;
using PG3302.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Nordly.Web.Pages;

public class IndexModel : PageModel
{
    private readonly OrderDbContext _db;

    public IndexModel(OrderDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<StoreProduct> Products => StorefrontCatalog.Products;
    public int CartCount { get; private set; }
    public IReadOnlyDictionary<Guid, int> ProductQuantities { get; private set; } = new Dictionary<Guid, int>();

    public void OnGet()
    {
        var cart = GetCart();
        CartCount = cart.Sum(item => item.Quantity);
        ProductQuantities = cart.ToDictionary(item => item.ProductId, item => item.Quantity);
    }

    public IActionResult OnPostAddToCart(Guid productId)
    {
        if (!StorefrontCatalog.Products.Any(product => product.Id == productId))
        {
            return NotFound();
        }

        var cart = GetCart();
        var item = cart.FirstOrDefault(cartItem => cartItem.ProductId == productId);

        if (item == null)
            cart.Add(new CartItem(productId, 1));
        else
            item.Quantity++;

        SaveCart(cart);
        if (IsAjaxRequest())
            return new JsonResult(new { success = true, quantity = cart.First(item => item.ProductId == productId).Quantity, cartCount = cart.Sum(item => item.Quantity) });

        TempData["Success"] = "Produktet er lagt i handlekurven";
        return RedirectToPage();
    }

    public IActionResult OnPostRemoveFromCart(Guid productId)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(cartItem => cartItem.ProductId == productId);

        if (item == null)
            return RedirectToPage();

        item.Quantity--;
        if (item.Quantity <= 0)
            cart.Remove(item);

        SaveCart(cart);
        if (IsAjaxRequest())
            return new JsonResult(new { success = true, quantity = cart.FirstOrDefault(item => item.ProductId == productId)?.Quantity ?? 0, cartCount = cart.Sum(item => item.Quantity) });

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
            return RedirectToPage();
        }

        try
        {
            var address = new MailAddress(email);
            if (string.IsNullOrWhiteSpace(address.Address))
            {
                TempData["NewsletterError"] = "Skriv inn en gyldig e-postadresse.";
                return RedirectToPage();
            }
        }
        catch
        {
            TempData["NewsletterError"] = "Skriv inn en gyldig e-postadresse.";
            return RedirectToPage();
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await _db.NewsletterSubscribers.AnyAsync(subscriber => subscriber.Email == normalizedEmail))
        {
            TempData["NewsletterError"] = "Denne e-posten er allerede registrert.";
            return RedirectToPage();
        }

        _db.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = normalizedEmail });
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["NewsletterError"] = "Denne e-posten er allerede registrert.";
            return RedirectToPage();
        }

        TempData["NewsletterSuccess"] = "Du er nå påmeldt vårt nyhetsbrev.";
        return RedirectToPage();
    }

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString("cart");
        return string.IsNullOrWhiteSpace(json)
            ? new List<CartItem>()
            : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
    }

    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetString("cart", JsonSerializer.Serialize(cart));
    }
}

public sealed class CartItem
{
    public CartItem(Guid productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }

    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
