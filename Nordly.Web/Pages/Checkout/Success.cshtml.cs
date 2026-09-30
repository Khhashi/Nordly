using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nordly.Web.Services;
using Stripe;
using Stripe.Checkout;

namespace Nordly.Web.Pages.Checkout;

public class SuccessModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly CheckoutOrderProcessor _processor;

    public SuccessModel(IConfiguration configuration, CheckoutOrderProcessor processor)
    {
        _configuration = configuration;
        _processor = processor;
    }

    public Guid? OrderId { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? session_id)
    {
        if (string.IsNullOrWhiteSpace(session_id) || string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]))
            return RedirectToPage("/Cart");

        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        var session = await new SessionService().GetAsync(session_id);
        var order = await _processor.ProcessPaidSessionAsync(session);
        if (order == null)
            return RedirectToPage("/Cart");

        HttpContext.Session.Remove("cart");
        OrderId = order.Id;
        return Page();
    }
}
