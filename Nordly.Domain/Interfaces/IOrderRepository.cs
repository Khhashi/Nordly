using Nordly.Domain.Entities;

namespace Nordly.Domain.Interfaces;

public interface IOrderRepository
{
    void Add(Order order);
    Order? GetById(Guid id);
    Order? GetByStripeCheckoutSessionId(string sessionId);
    List<Order> GetAll();
    void Update(Order order);
    void Delete(Guid id);
}