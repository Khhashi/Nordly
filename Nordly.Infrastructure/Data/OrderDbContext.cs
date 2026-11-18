using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nordly.Domain.Entities;

namespace Nordly.Infrastructure.Data;

public class OrderDbContext : IdentityUserContext<CustomerAccount>, IDataProtectionKeyContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<PasswordlessLoginChallenge> PasswordlessLoginChallenges => Set<PasswordlessLoginChallenge>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<DistributedCacheEntry> DistributedCacheEntries => Set<DistributedCacheEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerAccount>(entity =>
        {
            entity.HasIndex(user => user.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique();
            entity.HasIndex(user => user.CreatedAt);
            entity.Property(user => user.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<PasswordlessLoginChallenge>(entity =>
        {
            entity.HasKey(challenge => challenge.NormalizedEmail);
            entity.Property(challenge => challenge.NormalizedEmail).HasMaxLength(256);
            entity.Property(challenge => challenge.CodeHash).IsRequired().HasMaxLength(512);
            entity.Property(challenge => challenge.CreatedAt).IsRequired();
            entity.Property(challenge => challenge.ExpiresAt).IsRequired();
            entity.Property(challenge => challenge.FailedAttempts).IsRequired();
            entity.HasIndex(challenge => challenge.ExpiresAt);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).IsRequired().HasMaxLength(200);
            entity.Property(product => product.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.HasIndex(order => order.CreatedAt);
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
            entity.HasIndex(subscriber => subscriber.SubscribedAt);
            entity.Property(subscriber => subscriber.SubscribedAt).IsRequired();
            entity.Property(subscriber => subscriber.ConsentText).HasMaxLength(500);
            entity.Property(subscriber => subscriber.UnsubscribeVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<DistributedCacheEntry>(entity =>
        {
            entity.HasKey(entry => entry.Key);
            entity.Property(entry => entry.Key).IsRequired().HasMaxLength(512);
            entity.Property(entry => entry.Value).IsRequired();
            entity.Property(entry => entry.AbsoluteExpiration);
            entity.Property(entry => entry.ExpiresAt);
            entity.Property(entry => entry.SlidingExpiration);
            entity.Property(entry => entry.LastAccessed).IsRequired();
            entity.HasIndex(entry => entry.ExpiresAt);
        });
    }
}
