using Microsoft.Extensions.Logging.Abstractions;
using Nordly.Domain.Entities;
using Nordly.Domain.Services;
using Nordly.Web;
using Nordly.Web.Services;
using Stripe.Checkout;

namespace Nordly.Tests;

public class CheckoutOrderProcessorTests
{
    private sealed class FakeEmailSender : IOrderConfirmationEmailSender
    {
        public int SentCount { get; private set; }

        public Task SendAsync(Order order, CancellationToken cancellationToken = default)
        {
            SentCount++;
            return Task.CompletedTask;
        }
    }

    private static Session CreatePaidSession(string paymentStatus = "paid")
    {
        var cup = StorefrontCatalog.Products.First();
        return new Session
        {
            Id = "cs_test_1",
            PaymentStatus = paymentStatus,
            Metadata = new Dictionary<string, string>
            {
                ["product_ids"] = cup.Id.ToString(),
                ["quantities"] = "2"
            },
            CustomerDetails = new SessionCustomerDetails { Email = "kunde@example.com" }
        };
    }

    [Test]
    public async Task Same_Session_Twice_Creates_One_Order_And_Sends_One_Email()
    {
        var orderService = new OrderService(new FakeOrderRepository());
        var emailSender = new FakeEmailSender();
        var processor = new CheckoutOrderProcessor(orderService, emailSender, NullLogger<CheckoutOrderProcessor>.Instance);

        var first = await processor.ProcessPaidSessionAsync(CreatePaidSession());
        var second = await processor.ProcessPaidSessionAsync(CreatePaidSession());

        Assert.That(orderService.GetAllOrders(), Has.Count.EqualTo(1));
        Assert.That(second!.Id, Is.EqualTo(first!.Id));
        Assert.That(emailSender.SentCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Unpaid_Session_Does_Not_Create_Order()
    {
        var orderService = new OrderService(new FakeOrderRepository());
        var processor = new CheckoutOrderProcessor(orderService, new FakeEmailSender(), NullLogger<CheckoutOrderProcessor>.Instance);

        var order = await processor.ProcessPaidSessionAsync(CreatePaidSession("unpaid"));

        Assert.That(order, Is.Null);
        Assert.That(orderService.GetAllOrders(), Is.Empty);
    }

    [Test]
    public async Task Order_Gets_Products_And_Customer_From_Session()
    {
        var orderService = new OrderService(new FakeOrderRepository());
        var processor = new CheckoutOrderProcessor(orderService, new FakeEmailSender(), NullLogger<CheckoutOrderProcessor>.Instance);

        var order = await processor.ProcessPaidSessionAsync(CreatePaidSession());

        Assert.That(order!.OrderLines.Single().Quantity, Is.EqualTo(2));
        Assert.That(order.CustomerEmail, Is.EqualTo("kunde@example.com"));
        Assert.That(order.PaymentStatus, Is.EqualTo("Paid"));
    }
}
