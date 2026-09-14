namespace PG3302.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }

    public Product()
    {
        Id = Guid.NewGuid();
    }

    public Product(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.");

        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.");

        Id = Guid.NewGuid();
        Name = name;
        Price = price;
    }
}