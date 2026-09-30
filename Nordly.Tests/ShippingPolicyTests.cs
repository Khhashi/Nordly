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
}
