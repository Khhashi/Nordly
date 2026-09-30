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
}
