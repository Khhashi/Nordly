using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nordly.Domain.Entities;
using Nordly.Domain.Services;
using Nordly.Infrastructure.Data;

namespace Nordly.Web.Pages.Account;

[Authorize]
public sealed class IndexModel(
    UserManager<CustomerAccount> userManager,
    OrderService orderService) : PageModel
{
    public IReadOnlyList<Order> Orders { get; private set; } = Array.Empty<Order>();
    public string VerifiedEmail { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null || !user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
            return Forbid();

        VerifiedEmail = user.Email;
        Orders = orderService.GetOrdersByCustomerEmail(user.Email);
        return Page();
    }
}
