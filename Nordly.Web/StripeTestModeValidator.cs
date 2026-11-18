namespace Nordly.Web;

public static class StripeTestModeValidator
{
    public static void EnsureTestMode(string? secretKey)
    {
        if (!string.IsNullOrWhiteSpace(secretKey)
            && !secretKey.StartsWith("sk_test_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The Nordly demo only allows Stripe test-mode secret keys.");
        }
    }
}
