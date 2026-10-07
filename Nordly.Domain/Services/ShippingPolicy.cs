namespace Nordly.Domain.Services;

public static class ShippingPolicy
{
    public const decimal FreeShippingThreshold = 800m;
    public const decimal StandardShippingCost = 79m;

    public static decimal CalculateShippingCost(decimal orderTotal)
    {
        if (orderTotal < 0)
            throw new ArgumentOutOfRangeException(nameof(orderTotal), "Ordretotalen kan ikke være negativ.");

        return orderTotal >= FreeShippingThreshold ? 0m : StandardShippingCost;
    }

    public static decimal CalculateAmountUntilFreeShipping(decimal orderTotal)
    {
        if (orderTotal < 0)
            throw new ArgumentOutOfRangeException(nameof(orderTotal), "Ordretotalen kan ikke være negativ.");

        return Math.Max(0m, FreeShippingThreshold - orderTotal);
    }

    public static int CalculateFreeShippingProgressPercent(decimal orderTotal)
    {
        if (orderTotal < 0)
            throw new ArgumentOutOfRangeException(nameof(orderTotal), "Ordretotalen kan ikke være negativ.");

        return (int)Math.Min(100m, decimal.Floor(orderTotal / FreeShippingThreshold * 100m));
    }

    public static decimal CalculateFreeShippingProgressValue(decimal orderTotal)
    {
        if (orderTotal < 0)
            throw new ArgumentOutOfRangeException(nameof(orderTotal), "Ordretotalen kan ikke være negativ.");

        return Math.Min(orderTotal, FreeShippingThreshold);
    }
}
