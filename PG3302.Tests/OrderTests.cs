using NUnit.Framework;
using PG3302.Domain.Entities;

namespace PG3302.Tests;

public class OrderTests
{
    [Test]
    public void Order_Total_Should_Be_Calculated_For_Multiple_Products()
    {
        var laptop = new Product("Laptop", 15000);
        var mouse = new Product("Mouse", 500);

        var order = new Order();
        order.AddProduct(laptop, 1);
        order.AddProduct(mouse, 2);

        Assert.That(order.GetTotal(), Is.EqualTo(16000));
    }

    [Test]
    public void Adding_Product_With_Invalid_Quantity_Should_Throw_Exception()
    {
        var keyboard = new Product("Keyboard", 800);
        var order = new Order();

        Assert.Throws<ArgumentException>(() =>
        {
            order.AddProduct(keyboard, 0);
        });
    }

    [Test]
    public void Getting_Total_On_Empty_Order_Should_Throw_Exception()
    {
        var order = new Order();

        Assert.Throws<InvalidOperationException>(() =>
        {
            order.GetTotal();
        });
    }

    [Test]
    public void Order_Should_Report_Having_Products_When_Product_Added()
    {
        var headset = new Product("Headset", 1200);
        var order = new Order();

        order.AddProduct(headset, 1);

        Assert.That(order.HasProducts(), Is.True);
    }

    [Test]
    public void Removing_Product_Should_Remove_Correct_Product()
    {
        var laptop = new Product("Laptop", 15000);
        var mouse = new Product("Mouse", 500);

        var order = new Order();
        order.AddProduct(laptop, 1);
        order.AddProduct(mouse, 1);

        order.RemoveProduct(laptop.Id);

        Assert.That(order.OrderLines.Count, Is.EqualTo(1));
        Assert.That(order.OrderLines.First().Product.Name, Is.EqualTo("Mouse"));
    }
}