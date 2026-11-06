using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterSignupMessageTests
{
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
