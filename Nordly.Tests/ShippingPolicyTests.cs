using NUnit.Framework;
using Nordly.Domain.Services;

namespace Nordly.Tests;

public class ShippingPolicyTests
{
    [Test]
    public void Order_Under_Threshold_Should_Pay_Standard_Shipping()
    {
        Assert.That(ShippingPolicy.CalculateShippingCost(799m), Is.EqualTo(79m));
    }

    [Test]
    public void Order_Exactly_At_Threshold_Should_Get_Free_Shipping()
    {
        Assert.That(ShippingPolicy.CalculateShippingCost(800m), Is.EqualTo(0m));
    }

    [Test]
    public void Order_Above_Threshold_Should_Get_Free_Shipping()
    {
        Assert.That(ShippingPolicy.CalculateShippingCost(1500m), Is.EqualTo(0m));
    }

    [Test]
    public void Negative_Total_Should_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShippingPolicy.CalculateShippingCost(-1m));
    }

    [Test]
    public void Amount_Until_Free_Shipping_Should_Decrease_To_Zero_At_Threshold()
    {
        Assert.That(ShippingPolicy.CalculateAmountUntilFreeShipping(0m), Is.EqualTo(800m));
        Assert.That(ShippingPolicy.CalculateAmountUntilFreeShipping(550m), Is.EqualTo(250m));
        Assert.That(ShippingPolicy.CalculateAmountUntilFreeShipping(800m), Is.Zero);
        Assert.That(ShippingPolicy.CalculateAmountUntilFreeShipping(900m), Is.Zero);
    }

    [Test]
    public void Free_Shipping_Progress_Should_Stay_Between_Zero_And_One_Hundred()
    {
        Assert.That(ShippingPolicy.CalculateFreeShippingProgressPercent(0m), Is.Zero);
        Assert.That(ShippingPolicy.CalculateFreeShippingProgressPercent(399m), Is.EqualTo(49));
        Assert.That(ShippingPolicy.CalculateFreeShippingProgressPercent(799m), Is.EqualTo(99));
        Assert.That(ShippingPolicy.CalculateFreeShippingProgressPercent(800m), Is.EqualTo(100));
        Assert.That(ShippingPolicy.CalculateFreeShippingProgressPercent(900m), Is.EqualTo(100));
    }

    [Test]
    public void Free_Shipping_Progress_Should_Reject_Negative_Total()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShippingPolicy.CalculateAmountUntilFreeShipping(-1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShippingPolicy.CalculateFreeShippingProgressPercent(-1m));
    }

    [Test]
    public void Progress_Value_Should_Not_Exceed_Declared_Threshold()
    {
        Assert.That(ShippingPolicy.CalculateFreeShippingProgressValue(799m), Is.EqualTo(799m));
        Assert.That(ShippingPolicy.CalculateFreeShippingProgressValue(800m), Is.EqualTo(800m));
        Assert.That(ShippingPolicy.CalculateFreeShippingProgressValue(900m), Is.EqualTo(800m));
    }
}
