namespace Nordly.Web.Services;

public sealed record NewsletterSignupMessage(string Key, string Text)
{
    public static NewsletterSignupMessage FromOutcome(NewsletterSignupOutcome outcome) => outcome switch
    {
        NewsletterSignupOutcome.Subscribed =>
            new("NewsletterSuccess", "Du er nå påmeldt vårt nyhetsbrev."),
        NewsletterSignupOutcome.SubscribedEmailFailed =>
            new("NewsletterError", "Du er påmeldt nyhetsbrevet, men velkomst-e-posten kunne ikke sendes."),
        NewsletterSignupOutcome.ConsentRequired =>
            new("NewsletterError", "Du må samtykke for å melde deg på nyhetsbrevet."),
        NewsletterSignupOutcome.InvalidEmail =>
            new("NewsletterError", "Skriv inn en gyldig e-postadresse."),
        NewsletterSignupOutcome.AlreadySubscribed =>
            new("NewsletterError", "Denne e-posten er allerede registrert."),
        NewsletterSignupOutcome.InvalidConfiguration =>
            new("NewsletterError", "Påmeldingen kunne ikke fullføres akkurat nå. Prøv igjen senere."),
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };
}
