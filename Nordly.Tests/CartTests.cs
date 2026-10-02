using Microsoft.AspNetCore.Http;
using Nordly.Web;

namespace Nordly.Tests;

public class CartTests
{
    private static readonly Guid ProductId = Guid.NewGuid();

    [Test]
    public void Add_Same_Product_Twice_Increases_Quantity()
    {
        var cart = Cart.Empty();

        cart.Add(ProductId);
        cart.Add(ProductId);

        Assert.That(cart.Items, Has.Count.EqualTo(1));
        Assert.That(cart.QuantityOf(ProductId), Is.EqualTo(2));
    }

    [Test]
    public void Quantity_Is_Capped_At_Max()
    {
        var cart = Cart.Empty();

        cart.Add(ProductId, 8);
        cart.Add(ProductId, 5);

        Assert.That(cart.QuantityOf(ProductId), Is.EqualTo(Cart.MaxQuantityPerProduct));
    }

    [Test]
    public void Decrease_To_Zero_Removes_The_Product()
    {
        var cart = Cart.Empty();
        cart.Add(ProductId);

        cart.Decrease(ProductId);

        Assert.That(cart.Items, Is.Empty);
        Assert.That(cart.Count, Is.EqualTo(0));
    }

    [Test]
    public void Save_And_Load_Round_Trips_Through_Session()
    {
        var session = new FakeSession();
        var cart = Cart.Empty();
        cart.Add(ProductId, 3);

        cart.Save(session);
        var loaded = Cart.Load(session);

        Assert.That(loaded.QuantityOf(ProductId), Is.EqualTo(3));
    }

    [Test]
    public void Corrupt_Session_Data_Gives_Empty_Cart()
    {
        var session = new FakeSession();
        session.SetString("cart", "ikke json");

        Assert.That(Cart.Load(session).Items, Is.Empty);
    }

    private sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id => "test";
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
    }
}
