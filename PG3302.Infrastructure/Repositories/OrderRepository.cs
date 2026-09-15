using Microsoft.EntityFrameworkCore;
using PG3302.Domain.Entities;
using PG3302.Domain.Interfaces;
using PG3302.Infrastructure.Data;

namespace PG3302.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _db;

    public OrderRepository(OrderDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public void Add(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        _db.Orders.Add(order);
        _db.SaveChanges();
    }

    public Order? GetById(Guid id)
    {
        return _db.Orders
            .Include(order => order.OrderLines)
            .ThenInclude(line => line.Product)
            .SingleOrDefault(order => order.Id == id);
    }

    public List<Order> GetAll()
    {
        return _db.Orders
            .Include(order => order.OrderLines)
            .ThenInclude(line => line.Product)
            .OrderByDescending(order => order.CreatedAt)
            .ToList();
    }

    public void Update(Order order)
    {
        _db.Orders.Update(order);
        _db.SaveChanges();
    }

    public void Delete(Guid id)
    {
        var order = _db.Orders.Find(id) ?? throw new InvalidOperationException("Order not found.");
        _db.Orders.Remove(order);
        _db.SaveChanges();
    }
}