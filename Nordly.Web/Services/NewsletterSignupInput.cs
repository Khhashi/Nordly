using System.Net.Mail;

namespace Nordly.Web.Services;

public static class NewsletterSignupInput
{
    public const string ConsentText =
        "Ja, jeg samtykker til at NORDLY sender meg nyhetsbrev med nyheter og tilbud. Jeg kan melde meg av når som helst.";

    public static bool TryNormalizeEmail(string? email, out string normalizedEmail)
    {
        normalizedEmail = string.Empty;
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var trimmed = email.Trim();
        if (trimmed.Length > 320)
            return false;

        try
        {
            var address = new MailAddress(trimmed);
            if (!string.Equals(address.Address, trimmed, StringComparison.OrdinalIgnoreCase))
                return false;

            normalizedEmail = trimmed.ToLowerInvariant();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
