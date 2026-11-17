using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Nordly.Infrastructure.Data;
using Nordly.Web.Services;

namespace Nordly.Web.Pages.Account;

[EnableRateLimiting("passwordless-login")]
public sealed class LoginModel(
    PasswordlessLoginService loginService,
    SignInManager<CustomerAccount> signInManager) : PageModel
{
    [BindProperty]
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [BindProperty]
    public string? Code { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool CodeSent { get; private set; }
    public string? StatusMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnPostSendCodeAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Skriv inn en gyldig e-postadresse.";
            return Page();
        }

        var result = await loginService.RequestCodeAsync(Email, cancellationToken);
        switch (result)
        {
            case PasswordlessLoginRequestStatus.Sent:
            case PasswordlessLoginRequestStatus.Throttled:
                CodeSent = true;
                StatusMessage = "Hvis adressen kan brukes, sender vi en kode. Sjekk innboksen.";
                break;
            case PasswordlessLoginRequestStatus.InvalidEmail:
                ErrorMessage = "Skriv inn en gyldig e-postadresse.";
                break;
            case PasswordlessLoginRequestStatus.EmailUnavailable:
                ErrorMessage = "Innlogging via e-post er midlertidig utilgjengelig. Prøv igjen senere.";
                break;
            default:
                throw new InvalidOperationException("Unexpected passwordless login request status.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostVerifyCodeAsync(CancellationToken cancellationToken)
    {
        CodeSent = true;
        var user = await loginService.VerifyCodeAsync(Email, Code, cancellationToken);
        if (user is null)
        {
            ErrorMessage = "Koden er ugyldig eller utløpt. Be om en ny kode.";
            return Page();
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Url.IsLocalUrl(ReturnUrl)
            ? LocalRedirect(ReturnUrl!)
            : RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostSignOutAsync()
    {
        await signInManager.SignOutAsync();
        return RedirectToPage("/Index");
    }
}
