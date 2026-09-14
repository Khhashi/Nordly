using NUnit.Framework;
using PG3302.Domain.Entities;
using PG3302.Infrastructure.Repositories;
using System.IO;

namespace PG3302.Tests;

public class OrderRepositoryIntegrationTests
{
    private string _filePath = Path.Combine(AppContext.BaseDirectory, "orders.json");

    [SetUp]
    public void Setup()
    {
       
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    [Test]
    public void Repository_Should_Save_And_Load_Order_From_Json_File()
    {
        var repository = new OrderRepository();

        var order = new Order();
        order.AddProduct(new Product("IntegrationTestProduct", 100), 2);
        
        repository.Add(order);

        var loadedOrder = repository.GetById(order.Id);
        
        Assert.That(loadedOrder, Is.Not.Null);
        Assert.That(loadedOrder!.GetTotal(), Is.EqualTo(200));
    }
}