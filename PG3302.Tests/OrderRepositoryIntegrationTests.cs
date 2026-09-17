using NUnit.Framework;
using PG3302.Domain.Entities;
using PG3302.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using PG3302.Infrastructure.Data;

namespace PG3302.Tests;

public class OrderRepositoryIntegrationTests
{
    [Test]
    public void Repository_Should_Save_And_Load_Order_From_Database()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new OrderDbContext(options);
        var repository = new OrderRepository(db);

        var order = new Order();
        var product = new Product("IntegrationTestProduct", 100);
        db.Products.Add(product);
        db.SaveChanges();
        order.AddProduct(product, 2);
        
        repository.Add(order);

        var loadedOrder = repository.GetById(order.Id);
        
        Assert.That(loadedOrder, Is.Not.Null);
        Assert.That(loadedOrder!.GetTotal(), Is.EqualTo(200));
    }

    [Test]
    public void Repository_Should_Persist_Stripe_Payment_Data()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new OrderDbContext(options);
        var repository = new OrderRepository(db);
        var product = new Product("StripeTestProduct", 250);
        db.Products.Add(product);
        db.SaveChanges();

        var order = new Order
        {
            PaymentStatus = "Paid",
            StripeCheckoutSessionId = "cs_test_payment",
            StripePaymentIntentId = "pi_test_payment"
        };
        order.AddProduct(product, 1);

        repository.Add(order);

        var loadedOrder = repository.GetByStripeCheckoutSessionId("cs_test_payment");

        Assert.That(loadedOrder, Is.Not.Null);
        Assert.That(loadedOrder!.PaymentStatus, Is.EqualTo("Paid"));
        Assert.That(loadedOrder.StripePaymentIntentId, Is.EqualTo("pi_test_payment"));
    }

    [Test]
    public void Repository_Should_Find_Only_The_Matching_Stripe_Session()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new OrderDbContext(options);
        var repository = new OrderRepository(db);

        repository.Add(new Order { StripeCheckoutSessionId = "cs_test_first" });
        repository.Add(new Order { StripeCheckoutSessionId = "cs_test_second" });

        var found = repository.GetByStripeCheckoutSessionId("cs_test_second");

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.StripeCheckoutSessionId, Is.EqualTo("cs_test_second"));
    }
}