using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PG3302.Domain.Entities;
using PG3302.Domain.Services;
using Stripe;
using Stripe.Checkout;
using DomainProduct = PG3302.Domain.Entities.Product;

namespace Nordly.Web.Pages.Checkout;

public class SuccessModel : PageModel
{
    private readonly OrderService _service;
    private readonly IConfiguration _configuration;

    public SuccessModel(OrderService service, IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
    }

    public Guid? OrderId { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? session_id)
    {
        if (string.IsNullOrWhiteSpace(session_id) || string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]))
            return RedirectToPage("/Cart");

        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        var session = await new SessionService().GetAsync(session_id);
        if (session.PaymentStatus != "paid")
            return RedirectToPage("/Cart");

        var existingOrder = _service.GetOrderByStripeCheckoutSessionId(session.Id);
        if (existingOrder != null)
        {
            HttpContext.Session.Remove("cart");
            OrderId = existingOrder.Id;
            return Page();
        }

        var productIds = session.Metadata["product_ids"].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList();
        var quantities = session.Metadata["quantities"].Split(',').Select(int.Parse).ToList();
        var order = new Order();
        for (var index = 0; index < productIds.Count; index++)
        {
            var product = StorefrontCatalog.Products.FirstOrDefault(item => item.Id == productIds[index]);
            if (product != null)
                order.AddProduct(new DomainProduct(product.Name, product.Price), quantities[index]);
        }

        if (!order.HasProducts())
            return RedirectToPage("/Cart");

        order.PaymentStatus = "Paid";
        order.CustomerName = session.CustomerDetails?.Name;
        order.CustomerEmail = session.CustomerDetails?.Email;
        order.CustomerPhone = session.CustomerDetails?.Phone;
        order.ShippingAddress = FormatAddress(session.CustomerDetails?.Address);
        order.StripeCheckoutSessionId = session.Id;
        order.StripePaymentIntentId = session.PaymentIntentId;
        _service.CreateOrder(order);
        HttpContext.Session.Remove("cart");
        OrderId = order.Id;
        return Page();
    }

    private static string? FormatAddress(Address? address)
    {
        if (address == null)
            return null;

        return string.Join(", ", new[]
        {
            address.Line1,
            address.Line2,
            address.PostalCode,
            address.City,
            address.Country
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}