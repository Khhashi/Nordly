using NUnit.Framework;
using PG3302.Domain.Entities;
using PG3302.Domain.Services;

namespace PG3302.Tests;

public class OrderServiceTests
{
    private FakeOrderRepository _repository;
    private OrderService _service;

    [SetUp]
    public void Setup()
    {
        _repository = new FakeOrderRepository();
        _service = new OrderService(_repository);
    }

    [Test]
    public void CreateOrder_Should_Save_Order_When_Valid()
    {
        var laptop = new Product("Laptop", 15000);
        var order = new Order();
        order.AddProduct(laptop, 1);

        _service.CreateOrder(order);

        var saved = _repository.GetById(order.Id);

        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.OrderLines.Count, Is.EqualTo(1));
    }

    [Test]
    public void CreateOrder_Should_Throw_When_Order_Is_Empty()
    {
        var order = new Order();

        Assert.Throws<InvalidOperationException>(() =>
        {
            _service.CreateOrder(order);
        });
    }

    [Test]
    public void GetOrder_Should_Return_Correct_Order()
    {
        var keyboard = new Product("Keyboard", 800);
        var order = new Order();
        order.AddProduct(keyboard, 2);

        _service.CreateOrder(order);

        var found = _service.GetOrder(order.Id);

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.OrderLines.First().Product.Name, Is.EqualTo("Keyboard"));
    }

    [Test]
    public void GetOrderByStripeCheckoutSessionId_Should_Return_Existing_Order()
    {
        var product = new Product("Keyboard", 800);
        var order = new Order { StripeCheckoutSessionId = "cs_test_existing" };
        order.AddProduct(product, 1);
        _service.CreateOrder(order);

        var found = _service.GetOrderByStripeCheckoutSessionId("cs_test_existing");

        Assert.That(found, Is.SameAs(order));
    }

    [Test]
    public void GetAllOrders_Should_Return_All_Orders()
    {
        var monitor = new Product("Monitor", 3000);
        var headset = new Product("Headset", 1200);

        var order1 = new Order();
        order1.AddProduct(monitor, 1);

        var order2 = new Order();
        order2.AddProduct(headset, 3);

        _service.CreateOrder(order1);
        _service.CreateOrder(order2);

        var all = _service.GetAllOrders();

        Assert.That(all.Count, Is.EqualTo(2));
    }

    [Test]
    public void UpdateOrder_Should_Add_New_Product()
    {
        var laptop = new Product("Laptop", 15000);
        var mouse = new Product("Mouse", 500);

        var order = new Order();
        order.AddProduct(laptop, 1);

        _service.CreateOrder(order);

        order.AddProduct(mouse, 2);
        _service.UpdateOrder(order);

        var updated = _service.GetOrder(order.Id);

        Assert.That(updated!.OrderLines.Count, Is.EqualTo(2));
    }

    [Test]
    public void DeleteOrder_Should_Remove_Order()
    {
        var tablet = new Product("Tablet", 6000);
        var order = new Order();
        order.AddProduct(tablet, 1);

        _service.CreateOrder(order);
        _service.DeleteOrder(order.Id);

        var deleted = _service.GetOrder(order.Id);

        Assert.That(deleted, Is.Null);
    }
}