namespace PG3302.Domain.Entities;

public class OrderLine
{
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }

    public OrderLine()
    {
    }

    public OrderLine(Product product, int quantity)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        Product = product;
        Quantity = quantity;
    }
}