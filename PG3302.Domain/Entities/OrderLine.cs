namespace PG3302.Domain.Entities;

public class OrderLine
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }

    public OrderLine()
    {
        Id = Guid.NewGuid();
    }

    public OrderLine(Product product, int quantity)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        Product = product;
        ProductId = product.Id;
        Quantity = quantity;
    }
}