using Microsoft.EntityFrameworkCore;
using Nordly.Domain.Entities;
using Nordly.Domain.Services;
using Stripe;
using Stripe.Checkout;
using DomainProduct = Nordly.Domain.Entities.Product;

namespace Nordly.Web.Services;

public class CheckoutOrderProcessor
{
    private readonly OrderService _orderService;
    private readonly IOrderConfirmationEmailSender _emailSender;
    private readonly ILogger<CheckoutOrderProcessor> _logger;

    public CheckoutOrderProcessor(
        OrderService orderService,
        IOrderConfirmationEmailSender emailSender,
        ILogger<CheckoutOrderProcessor> logger)
    {
        _orderService = orderService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Order?> ProcessPaidSessionAsync(Session session)
    {
        if (session.PaymentStatus != "paid")
            return null;

        var existingOrder = _orderService.GetOrderByStripeCheckoutSessionId(session.Id);
        if (existingOrder != null)
            return existingOrder;

        var order = CreateOrderFromSession(session);
        if (!order.HasProducts())
            return null;

        bool created;
        try
        {
            created = _orderService.RecordPaidCheckout(order, session.Id);
        }
        catch (DbUpdateException)
        {
            // Nettsiden og webhooken kom samtidig; den andre lagret ordren først.
            created = false;
        }

        if (!created)
            return _orderService.GetOrderByStripeCheckoutSessionId(session.Id);

        try
        {
            await _emailSender.SendAsync(order);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not send order confirmation email for order {OrderId}.", order.Id);
        }

        return order;
    }

    private static Order CreateOrderFromSession(Session session)
    {
        var order = new Order();
        if (session.Metadata == null
            || !session.Metadata.TryGetValue("product_ids", out var productIdsValue)
            || !session.Metadata.TryGetValue("quantities", out var quantitiesValue))
            return order;

        var productIds = productIdsValue.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList();
        var quantities = quantitiesValue.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
        for (var index = 0; index < productIds.Count && index < quantities.Count; index++)
        {
            var product = StorefrontCatalog.Products.FirstOrDefault(item => item.Id == productIds[index]);
            if (product != null)
                order.AddProduct(new DomainProduct(product.Name, product.Price), quantities[index]);
        }

        order.CustomerName = session.CollectedInformation?.ShippingDetails?.Name ?? session.CustomerDetails?.Name;
        order.CustomerEmail = session.CustomerDetails?.Email;
        order.CustomerPhone = session.CustomerDetails?.Phone;
        order.ShippingAddress = FormatAddress(session.CollectedInformation?.ShippingDetails?.Address ?? session.CustomerDetails?.Address);
        order.ShippingCost = (session.TotalDetails?.AmountShipping ?? 0) / 100m;
        order.StripePaymentIntentId = session.PaymentIntentId;
        return order;
    }

    private static string? FormatAddress(Address? address)
    {
        if (address == null)
            return null;

        return string.Join(", ", new[]
        {
            address.Line1,
            address.Line2,
            address.PostalCode,
            address.City,
            address.Country
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
