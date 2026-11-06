using Nordly.Web.Services;

namespace Nordly.Tests;

public class NewsletterSignupInputTests
{
    [Test]
    public void Normalizes_Email_Before_Storing()
    {
        var valid = NewsletterSignupInput.TryNormalizeEmail("  Customer@Example.com  ", out var email);

        Assert.That(valid, Is.True);
        Assert.That(email, Is.EqualTo("customer@example.com"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not-an-address")]
    [TestCase("Customer <customer@example.com>")]
    [TestCase("customer@example.com, other@example.com")]
    public void Rejects_Invalid_Or_Ambiguous_Email(string? input)
    {
        var valid = NewsletterSignupInput.TryNormalizeEmail(input, out var email);

        Assert.That(valid, Is.False);
        Assert.That(email, Is.Empty);
    }

    [Test]
    public void Rejects_Address_Longer_Than_Database_Limit()
    {
        var input = new string('a', 310) + "@example.com";

        Assert.That(NewsletterSignupInput.TryNormalizeEmail(input, out _), Is.False);
    }
}
