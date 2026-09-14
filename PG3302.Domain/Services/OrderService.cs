using PG3302.Domain.Entities;
using PG3302.Domain.Interfaces;

namespace PG3302.Domain.Services;

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

    public Order? GetOrder(Guid id)
    {
        return _repository.GetById(id);
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