using PG3302.Domain.Entities;

namespace PG3302.Domain.Interfaces;

public interface IOrderRepository
{
    void Add(Order order);
    Order? GetById(Guid id);
    Order? GetByStripeCheckoutSessionId(string sessionId);
    List<Order> GetAll();
    void Update(Order order);
    void Delete(Guid id);
}