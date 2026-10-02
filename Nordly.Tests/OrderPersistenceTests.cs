using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Domain.Entities;
using Nordly.Domain.Interfaces;
using Nordly.Domain.Services;
using Nordly.Infrastructure.Data;
using Nordly.Infrastructure.Repositories;
using Nordly.Web;
using Nordly.Web.Services;
using Stripe.Checkout;

namespace Nordly.Tests;

public class OrderPersistenceTests
{
    private sealed class CountingEmailSender : IOrderConfirmationEmailSender
    {
        public int SentCount { get; private set; }

        public Task SendAsync(Order order, CancellationToken cancellationToken = default)
        {
            SentCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingOrderRepository : IOrderRepository
    {
        public void Add(Order order) => throw new DbUpdateException("Databasen er nede.");
        public Order? GetById(Guid id) => null;
        public Order? GetByStripeCheckoutSessionId(string sessionId) => null;
        public List<Order> GetAll() => new();
        public void Update(Order order) { }
        public void Delete(Guid id) { }
    }

    private static Session CreatePaidSession()
    {
        var cup = StorefrontCatalog.Products.First();
        return new Session
        {
            Id = "cs_test_persistence",
            PaymentStatus = "paid",
            Metadata = new Dictionary<string, string>
            {
                ["product_ids"] = cup.Id.ToString(),
                ["quantities"] = "1"
            },
            CustomerDetails = new SessionCustomerDetails { Email = "kunde@example.com" }
        };
    }

    [Test]
    public void Database_Error_Without_Saved_Order_Is_Thrown_So_Stripe_Retries()
    {
        var emailSender = new CountingEmailSender();
        var processor = new CheckoutOrderProcessor(
            new OrderService(new FailingOrderRepository()),
            emailSender,
            NullLogger<CheckoutOrderProcessor>.Instance);

        Assert.ThrowsAsync<DbUpdateException>(() => processor.ProcessPaidSessionAsync(CreatePaidSession()));
        Assert.That(emailSender.SentCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Order_Lines_Use_The_Catalog_Product_Id()
    {
        var processor = new CheckoutOrderProcessor(
            new OrderService(new FakeOrderRepository()),
            new CountingEmailSender(),
            NullLogger<CheckoutOrderProcessor>.Instance);

        var order = await processor.ProcessPaidSessionAsync(CreatePaidSession());

        Assert.That(order!.OrderLines.Single().ProductId, Is.EqualTo(StorefrontCatalog.Products.First().Id));
    }

    [Test]
    public void Two_Orders_With_Same_Product_Do_Not_Duplicate_The_Product()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var catalogProduct = new Product("Alba keramikk kopp", 249);
        using (var seedDb = new OrderDbContext(options))
        {
            seedDb.Products.Add(catalogProduct);
            seedDb.SaveChanges();
        }

        for (var i = 0; i < 2; i++)
        {
            using var db = new OrderDbContext(options);
            var order = new Order();
            var copy = new Product(catalogProduct.Name, catalogProduct.Price) { Id = catalogProduct.Id };
            order.AddProduct(copy, 1);
            new OrderRepository(db).Add(order);
        }

        using var verifyDb = new OrderDbContext(options);
        Assert.That(verifyDb.Products.Count(), Is.EqualTo(1));
        Assert.That(verifyDb.Orders.Count(), Is.EqualTo(2));
    }
}
