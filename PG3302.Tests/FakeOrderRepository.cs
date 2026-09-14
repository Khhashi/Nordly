using PG3302.Domain.Entities;
using PG3302.Domain.Interfaces;

namespace PG3302.Tests;

public class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = new();

    public void Add(Order order)
    {
        _orders[order.Id] = order;
    }

    public Order? GetById(Guid id)
    {
        return _orders.ContainsKey(id) ? _orders[id] : null;
    }

    public List<Order> GetAll()
    {
        return _orders.Values.ToList();
    }

    public void Update(Order order)
    {
        if (_orders.ContainsKey(order.Id))
        {
            _orders[order.Id] = order;
        }
    }

    public void Delete(Guid id)
    {
        if (_orders.ContainsKey(id))
        {
            _orders.Remove(id);
        }
    }
}