using Nordly.Domain.Entities;
using Nordly.Domain.Interfaces;

namespace Nordly.Domain.Services;

public class OrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public void CreateOrder(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        if (!order.HasProducts())
            throw new InvalidOperationException("Order must contain at least one item.");

        _repository.Add(order);
    }

    public bool RecordPaidCheckout(Order order, string checkoutSessionId)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        if (string.IsNullOrWhiteSpace(checkoutSessionId))
            throw new ArgumentException("Stripe checkout session ID is required.", nameof(checkoutSessionId));

        if (_repository.GetByStripeCheckoutSessionId(checkoutSessionId) != null)
            return false;

        order.StripeCheckoutSessionId = checkoutSessionId;
        order.PaymentStatus = "Paid";
        CreateOrder(order);
        return true;
    }

    public Order? GetOrder(Guid id)
    {
        return _repository.GetById(id);
    }

    public Order? GetOrderByStripeCheckoutSessionId(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Stripe checkout session ID is required.", nameof(sessionId));

        return _repository.GetByStripeCheckoutSessionId(sessionId);
    }

    public List<Order> GetAllOrders()
    {
        return _repository.GetAll();
    }

    public void UpdateOrder(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        if (!order.HasProducts())
            throw new InvalidOperationException("Cannot update order without products.");

        var existing = _repository.GetById(order.Id);

        if (existing == null)
            throw new InvalidOperationException("Order does not exist.");

        _repository.Update(order);
    }

    public void DeleteOrder(Guid id)
    {
        var existing = _repository.GetById(id);

        if (existing == null)
            throw new InvalidOperationException("Order does not exist.");

        _repository.Delete(id);
    }
}