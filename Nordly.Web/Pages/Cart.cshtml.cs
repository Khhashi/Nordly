using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nordly.Domain.Entities;
using Nordly.Domain.Services;
using Stripe;
using Stripe.Checkout;

namespace Nordly.Web.Pages;

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
    public IReadOnlyList<StoreProduct> SuggestedProducts => StorefrontCatalog.Products.Take(4).ToArray();
    public decimal Total => Items.Sum(item => item.Product.Price * item.Quantity);
    public decimal FreeShippingThreshold => ShippingPolicy.FreeShippingThreshold;
    public decimal FreeShippingProgressValue => ShippingPolicy.CalculateFreeShippingProgressValue(Total);
    public decimal ShippingCost => ShippingPolicy.CalculateShippingCost(Total);
    public decimal AmountUntilFreeShipping => ShippingPolicy.CalculateAmountUntilFreeShipping(Total);
    public int FreeShippingProgressPercent => ShippingPolicy.CalculateFreeShippingProgressPercent(Total);
    public bool HasFreeShipping => Total >= ShippingPolicy.FreeShippingThreshold;
    public string ShippingLabel => ShippingCost == 0m ? "Gratis levering" : "Standard levering";
    public decimal GrandTotal => Total + ShippingCost;
    public bool StripeConfigured => !string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]);

    public void OnGet() => LoadCart();

    public IActionResult OnPostIncrease(Guid productId)
    {
        var cart = Cart.Load(HttpContext.Session);
        if (cart.QuantityOf(productId) > 0)
            cart.Add(productId);
        cart.Save(HttpContext.Session);
        TempData["Success"] = "Antallet er oppdatert.";
        return RedirectToPage();
    }

    public IActionResult OnPostDecrease(Guid productId)
    {
        var cart = Cart.Load(HttpContext.Session);
        cart.Decrease(productId);
        cart.Save(HttpContext.Session);
        TempData["Success"] = "Antallet er oppdatert.";
        return RedirectToPage();
    }

    public IActionResult OnPostRemove(Guid productId)
    {
        var cart = Cart.Load(HttpContext.Session);
        cart.Remove(productId);
        cart.Save(HttpContext.Session);
        TempData["Success"] = "Produktet er fjernet fra handlekurven.";
        return RedirectToPage();
    }

    public IActionResult OnPostAddSuggestedProduct(Guid productId)
    {
        if (!StorefrontCatalog.Products.Any(product => product.Id == productId))
            return NotFound();

        var cart = Cart.Load(HttpContext.Session);
        cart.Add(productId);
        cart.Save(HttpContext.Session);
        TempData["Success"] = "Produktet er lagt i handlekurven.";
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
            BillingAddressCollection = "required",
            PhoneNumberCollection = new SessionPhoneNumberCollectionOptions
            {
                Enabled = true
            },
            ShippingAddressCollection = new SessionShippingAddressCollectionOptions
            {
                AllowedCountries = new List<string> { "NO" }
            },
            ShippingOptions = new List<SessionShippingOptionOptions>
            {
                new()
                {
                    ShippingRateData = new SessionShippingOptionShippingRateDataOptions
                    {
                        Type = "fixed_amount",
                        DisplayName = ShippingLabel,
                        FixedAmount = new SessionShippingOptionShippingRateDataFixedAmountOptions
                        {
                            Amount = (long)(ShippingCost * 100),
                            Currency = "nok"
                        },
                        DeliveryEstimate = new SessionShippingOptionShippingRateDataDeliveryEstimateOptions
                        {
                            Minimum = new SessionShippingOptionShippingRateDataDeliveryEstimateMinimumOptions
                            {
                                Unit = "business_day",
                                Value = 2
                            },
                            Maximum = new SessionShippingOptionShippingRateDataDeliveryEstimateMaximumOptions
                            {
                                Unit = "business_day",
                                Value = 4
                            }
                        }
                    }
                }
            },
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
        Items = Cart.Load(HttpContext.Session).Items
            .Select(item =>
            {
                var product = StorefrontCatalog.Products.FirstOrDefault(candidate => candidate.Id == item.ProductId);
                return product == null ? null : new CartViewItem(product, item.Quantity);
            })
            .Where(item => item != null)
            .Cast<CartViewItem>()
            .ToList();
    }
}

public record CartViewItem(StoreProduct Product, int Quantity);