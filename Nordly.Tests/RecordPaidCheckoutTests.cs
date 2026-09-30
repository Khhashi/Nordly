using NUnit.Framework;
using Nordly.Domain.Entities;
using Nordly.Domain.Services;

namespace Nordly.Tests;

public class RecordPaidCheckoutTests
{
    private FakeOrderRepository _repository;
    private OrderService _service;

    [SetUp]
    public void Setup()
    {
        _repository = new FakeOrderRepository();
        _service = new OrderService(_repository);
    }

    private static Order CreateOrder()
    {
        var order = new Order();
        order.AddProduct(new Product("Alba keramikk kopp", 249), 1);
        return order;
    }

    [Test]
    public void First_Call_Should_Create_Paid_Order()
    {
        var created = _service.RecordPaidCheckout(CreateOrder(), "cs_test_1");

        var saved = _repository.GetByStripeCheckoutSessionId("cs_test_1");
        Assert.That(created, Is.True);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.PaymentStatus, Is.EqualTo("Paid"));
    }

    [Test]
    public void Same_Session_Twice_Should_Create_Only_One_Order()
    {
        var first = _service.RecordPaidCheckout(CreateOrder(), "cs_test_1");
        var second = _service.RecordPaidCheckout(CreateOrder(), "cs_test_1");

        Assert.That(first, Is.True);
        Assert.That(second, Is.False);
        Assert.That(_repository.GetAll(), Has.Count.EqualTo(1));
    }

    [Test]
    public void Missing_Session_Id_Should_Throw()
    {
        Assert.Throws<ArgumentException>(() => _service.RecordPaidCheckout(CreateOrder(), ""));
    }
}
