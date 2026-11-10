using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterSignupMessageTests
{
    [Test]
    public void Every_Outcome_Has_A_Visible_Message()
    {
        foreach (var outcome in Enum.GetValues<NewsletterSignupOutcome>())
            Assert.That(NewsletterSignupMessage.FromOutcome(outcome).Text, Is.Not.Empty);
    }

    [TestCase(NewsletterSignupOutcome.ConsentRequired, "samtykke")]
    [TestCase(NewsletterSignupOutcome.InvalidEmail, "gyldig e-postadresse")]
    [TestCase(NewsletterSignupOutcome.AlreadySubscribed, "allerede registrert")]
    [TestCase(NewsletterSignupOutcome.InvalidConfiguration, "ikke fullføres")]
    public void Rejected_Signups_Use_An_Explanatory_Error(
        NewsletterSignupOutcome outcome, string expectedText)
    {
        var message = NewsletterSignupMessage.FromOutcome(outcome);

        Assert.That(message.Key, Is.EqualTo("NewsletterError"));
        Assert.That(message.Text, Does.Contain(expectedText));
    }

    [Test]
    public void Unknown_Outcome_Is_Not_Reported_As_Success()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NewsletterSignupMessage.FromOutcome((NewsletterSignupOutcome)999));
    }

    [Test]
    public void Email_Failure_Reports_Both_Subscription_And_Delivery_Failure()
    {
        var message = NewsletterSignupMessage.FromOutcome(NewsletterSignupOutcome.SubscribedEmailFailed);

        Assert.That(message.Key, Is.EqualTo("NewsletterError"));
        Assert.That(message.Text, Does.Contain("Du er påmeldt"));
        Assert.That(message.Text, Does.Contain("kunne ikke sendes"));
    }

    [Test]
    public void Successful_Subscription_Uses_Success_Message()
    {
        var message = NewsletterSignupMessage.FromOutcome(NewsletterSignupOutcome.Subscribed);
        Assert.That(message.Key, Is.EqualTo("NewsletterSuccess"));
        Assert.That(message.Text, Does.Contain("påmeldt"));
    }
}
