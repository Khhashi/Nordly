using Nordly.Web;

namespace Nordly.Tests;

[TestFixture]
public sealed class StripeTestModeValidatorTests
{
    [Test]
    public void Test_Mode_Key_Is_Allowed()
    {
        Assert.DoesNotThrow(() => StripeTestModeValidator.EnsureTestMode("sk_test_example"));
    }

    [TestCase("sk_live_example")]
    [TestCase("rk_test_example")]
    public void Non_Test_Mode_Keys_Are_Rejected(string secretKey)
    {
        Assert.Throws<InvalidOperationException>(() => StripeTestModeValidator.EnsureTestMode(secretKey));
    }

    [Test]
    public void Missing_Key_Is_Left_To_Environment_Configuration_Validation()
    {
        Assert.DoesNotThrow(() => StripeTestModeValidator.EnsureTestMode(null));
        Assert.DoesNotThrow(() => StripeTestModeValidator.EnsureTestMode(""));
    }
}
