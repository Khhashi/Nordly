using Microsoft.EntityFrameworkCore;
using PG3302.Domain.Entities;

namespace PG3302.Infrastructure.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).IsRequired().HasMaxLength(200);
            entity.Property(product => product.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.PaymentStatus).IsRequired().HasMaxLength(40);
            entity.Property(order => order.CustomerName).HasMaxLength(200);
            entity.Property(order => order.CustomerEmail).HasMaxLength(320);
            entity.Property(order => order.CustomerPhone).HasMaxLength(40);
            entity.Property(order => order.ShippingAddress).HasMaxLength(1000);
            entity.Property(order => order.ShippingCost).HasPrecision(18, 2);
            entity.Property(order => order.StripeCheckoutSessionId).HasMaxLength(200);
            entity.Property(order => order.StripePaymentIntentId).HasMaxLength(200);
            entity.HasIndex(order => order.StripeCheckoutSessionId).IsUnique();
            entity.HasMany(order => order.OrderLines)
                .WithOne(line => line.Order)
                .HasForeignKey(line => line.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.HasKey(line => line.Id);
            entity.HasOne(line => line.Product)
                .WithMany()
                .HasForeignKey(line => line.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(line => line.Quantity).IsRequired();
        });

        modelBuilder.Entity<NewsletterSubscriber>(entity =>
        {
            entity.HasKey(subscriber => subscriber.Id);
            entity.Property(subscriber => subscriber.Email).IsRequired().HasMaxLength(320);
            entity.HasIndex(subscriber => subscriber.Email).IsUnique();
            entity.Property(subscriber => subscriber.SubscribedAt).IsRequired();
        });
    }
}
