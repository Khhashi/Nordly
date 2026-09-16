using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PG3302.Domain.Entities;
using PG3302.Domain.Services;
using Stripe;
using Stripe.Checkout;

namespace OrderManager.Web.Pages;

public class CartModel : PageModel
{
    private readonly OrderService _service;
    private readonly IConfiguration _configuration;

    public CartModel(OrderService service, IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
    }

    public List<CartViewItem> Items { get; private set; } = new();
    public decimal Total => Items.Sum(item => item.Product.Price * item.Quantity);
    public bool StripeConfigured => !string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]);

    public void OnGet() => LoadCart();

    public IActionResult OnPostIncrease(Guid productId)
    {
        UpdateQuantity(productId, 1);
        TempData["Success"] = "Antallet er oppdatert.";
        return RedirectToPage();
    }

    public IActionResult OnPostDecrease(Guid productId)
    {
        UpdateQuantity(productId, -1);
        TempData["Success"] = "Antallet er oppdatert.";
        return RedirectToPage();
    }

    public IActionResult OnPostRemove(Guid productId)
    {
        var cart = ReadCart();
        cart.RemoveAll(item => item.ProductId == productId);
        SaveCart(cart);
        TempData["Success"] = "Produktet er fjernet fra handlekurven.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCheckout()
    {
        LoadCart();

        if (!Items.Any())
        {
            TempData["Error"] = "Handlekurven er tom.";
            return RedirectToPage();
        }

        var secretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            TempData["CheckoutNotice"] = "Stripe-betaling er ikke aktivert ennå.";
            return RedirectToPage();
        }

        StripeConfiguration.ApiKey = secretKey;
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            PaymentMethodTypes = new List<string> { "card" },
            ClientReferenceId = HttpContext.Session.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            LineItems = Items.Select(item => new SessionLineItemOptions
            {
                Quantity = item.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "nok",
                    UnitAmount = (long)(item.Product.Price * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.Product.Name
                    }
                }
            }).ToList(),
            Metadata = new Dictionary<string, string>
            {
                ["cart_session_id"] = HttpContext.Session.Id,
                ["product_ids"] = string.Join(",", Items.Select(item => item.Product.Id)),
                ["quantities"] = string.Join(",", Items.Select(item => item.Quantity))
            },
            SuccessUrl = $"{Request.Scheme}://{Request.Host}/Checkout/Success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{Request.Scheme}://{Request.Host}/Cart"
        };

        try
        {
            var session = await new SessionService().CreateAsync(options);
            return Redirect(session.Url);
        }
        catch (StripeException)
        {
            TempData["CheckoutNotice"] = "Stripe-betaling kunne ikke startes akkurat nå.";
            return RedirectToPage();
        }
    }

    private void LoadCart()
    {
        var cart = ReadCart();

        Items = cart
            .Select(item =>
            {
                var product = StorefrontCatalog.Products.FirstOrDefault(candidate => candidate.Id == item.ProductId);
                return product == null ? null : new CartViewItem(product, item.Quantity);
            })
            .Where(item => item != null)
            .Cast<CartViewItem>()
            .ToList();
    }

    private void UpdateQuantity(Guid productId, int change)
    {
        var cart = ReadCart();
        var item = cart.FirstOrDefault(cartItem => cartItem.ProductId == productId);
        if (item == null)
            return;

        item.Quantity += change;
        if (item.Quantity <= 0)
            cart.Remove(item);

        SaveCart(cart);
    }

    private List<CartItem> ReadCart()
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

public record CartViewItem(StoreProduct Product, int Quantity);