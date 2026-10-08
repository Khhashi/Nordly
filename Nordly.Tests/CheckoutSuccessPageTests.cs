using Nordly.Domain.Entities;
using Nordly.Web.Pages.Checkout;

namespace Nordly.Tests;

public class CheckoutSuccessPageTests
{
    [Test]
    public void Formatted_Order_Number_Is_Null_When_No_Order_Was_Provided()
    {
        Assert.That(SuccessModel.FormatOrderNumber(null), Is.Null);
    }

    [Test]
    public void Formatted_Order_Number_Uses_The_Public_Order_Number_When_Order_Exists()
    {
        var orderId = Guid.Parse("781c7228-1234-4abc-9def-000000000000");

        Assert.That(SuccessModel.FormatOrderNumber(orderId), Is.EqualTo(Order.FormatNumber(orderId)));
        Assert.That(SuccessModel.FormatOrderNumber(orderId), Is.EqualTo("781C7228"));
    }
}
