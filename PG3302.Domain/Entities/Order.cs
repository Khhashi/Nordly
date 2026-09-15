namespace PG3302.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }

    public List<OrderLine> OrderLines { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Order()
    {
        Id = Guid.NewGuid();
    }

    public void AddProduct(Product product, int quantity)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        OrderLines.Add(new OrderLine(product, quantity));
    }

    public void RemoveProduct(Guid productId)
    {
        var line = OrderLines.FirstOrDefault(x => x.Product.Id == productId);

        if (line == null)
            throw new InvalidOperationException("Product not found in order.");

        OrderLines.Remove(line);
    }

    public bool HasProducts()
    {
        return OrderLines.Any();
    }

    public decimal GetTotal()
    {
        if (!HasProducts())
            throw new InvalidOperationException("Cannot calculate total for empty order.");

        return OrderLines.Sum(x => x.Product.Price * x.Quantity);
    }
}