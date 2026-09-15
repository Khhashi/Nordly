using NUnit.Framework;
using PG3302.Domain.Entities;
using PG3302.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using PG3302.Infrastructure.Data;

namespace PG3302.Tests;

public class OrderRepositoryIntegrationTests
{
    [Test]
    public void Repository_Should_Save_And_Load_Order_From_Database()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new OrderDbContext(options);
        var repository = new OrderRepository(db);

        var order = new Order();
        var product = new Product("IntegrationTestProduct", 100);
        db.Products.Add(product);
        db.SaveChanges();
        order.AddProduct(product, 2);
        
        repository.Add(order);

        var loadedOrder = repository.GetById(order.Id);
        
        Assert.That(loadedOrder, Is.Not.Null);
        Assert.That(loadedOrder!.GetTotal(), Is.EqualTo(200));
    }
}