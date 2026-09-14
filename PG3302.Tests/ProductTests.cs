
using NUnit.Framework;
using PG3302.Domain.Entities;

namespace PG3302.Tests;

public class ProductTests
{
    [Test]
    public void Creating_Product_With_Valid_Data_Should_Work()
    {
        var product = new Product("Smartphone", 9000);

        Assert.That(product.Name, Is.EqualTo("Smartphone"));
        Assert.That(product.Price, Is.EqualTo(9000));
    }

    [Test]
    public void Creating_Product_With_Zero_Price_Should_Throw_Exception()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new Product("Monitor", 0);
        });
    }

    [Test]
    public void Creating_Product_With_Empty_Name_Should_Throw_Exception()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new Product("", 1000);
        });
    }

    [Test]
    public void Creating_Product_With_Null_Name_Should_Throw_Exception()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new Product(null!, 1000);
        });
    }
}