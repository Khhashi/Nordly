using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Nordly.Domain.Services;
using Nordly.Web;
using Nordly.Web.Pages;

namespace Nordly.Tests;

public class CartPageTests
{
    [Test]
    public void Suggested_Products_Are_Limited_To_Existing_Catalog_Products()
    {
        var model = CreateModel(new FakeSession());

        Assert.That(model.SuggestedProducts, Has.Count.EqualTo(4));
        Assert.That(model.SuggestedProducts.Select(product => product.Id),
            Is.SubsetOf(StorefrontCatalog.Products.Select(product => product.Id)));
    }

    [Test]
    public void Add_Suggested_Product_Stores_It_In_The_Cart_And_Redirects()
    {
        var session = new FakeSession();
        var model = CreateModel(session);
        var productId = model.SuggestedProducts[0].Id;

        var result = model.OnPostAddSuggestedProduct(productId);

        Assert.That(Cart.Load(session).QuantityOf(productId), Is.EqualTo(1));
        Assert.That(result, Is.TypeOf<RedirectToPageResult>());
    }

    [Test]
    public void Add_Unknown_Suggested_Product_Returns_Not_Found()
    {
        var session = new FakeSession();
        var model = CreateModel(session);

        var result = model.OnPostAddSuggestedProduct(Guid.NewGuid());

        Assert.That(result, Is.TypeOf<NotFoundResult>());
        Assert.That(Cart.Load(session).Items, Is.Empty);
    }

    [Test]
    public void Sale_Product_Uses_Current_Price_For_Cart_Total_And_Shipping()
    {
        var session = new FakeSession();
        var saleProduct = StorefrontCatalog.Products.Single(product => product.Name == "Forma ullteppe");
        var cart = Cart.Empty();
        cart.Add(saleProduct.Id);
        cart.Save(session);
        var model = CreateModel(session);

        model.OnGet();

        Assert.That(saleProduct.OriginalPrice, Is.EqualTo(899m));
        Assert.That(saleProduct.Price, Is.EqualTo(699m));
        Assert.That(model.Total, Is.EqualTo(699m));
        Assert.That(model.ShippingCost, Is.EqualTo(ShippingPolicy.StandardShippingCost));
        Assert.That(model.GrandTotal, Is.EqualTo(699m + ShippingPolicy.StandardShippingCost));
    }

    [Test]
    public void Get_Restores_Valid_Checkout_Cart_When_Session_Is_Empty()
    {
        var session = new FakeSession();
        var model = CreateModel(session);
        var product = StorefrontCatalog.Products[0];

        model.OnGet($"{product.Id:N}:2");

        Assert.That(model.Items.Single().Product.Id, Is.EqualTo(product.Id));
        Assert.That(model.Items.Single().Quantity, Is.EqualTo(2));
        Assert.That(Cart.Load(session).QuantityOf(product.Id), Is.EqualTo(2));
    }

    [Test]
    public void Get_Does_Not_Overwrite_Existing_Cart_With_Checkout_Snapshot()
    {
        var session = new FakeSession();
        var existingProduct = StorefrontCatalog.Products[0];
        var restoredProduct = StorefrontCatalog.Products[1];
        var cart = Cart.Empty();
        cart.Add(existingProduct.Id);
        cart.Save(session);
        var model = CreateModel(session);

        model.OnGet($"{restoredProduct.Id:N}:2");

        Assert.That(model.Items.Select(item => item.Product.Id), Is.EqualTo(new[] { existingProduct.Id }));
    }

    [Test]
    public void Get_Rejects_Invalid_Checkout_Cart_Snapshot()
    {
        var session = new FakeSession();
        var model = CreateModel(session);

        model.OnGet($"{Guid.NewGuid():N}:1");

        Assert.That(model.Items, Is.Empty);
        Assert.That(model.TempData["Error"], Is.EqualTo("Handlekurven kunne ikke gjenopprettes."));
        Assert.That(Cart.Load(session).Items, Is.Empty);
    }

    private static CartModel CreateModel(FakeSession session)
    {
        var httpContext = new DefaultHttpContext { Session = session };
        var model = new CartModel(new OrderService(new FakeOrderRepository()), new ConfigurationBuilder().Build())
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new FakeTempDataProvider())
        };
        return model;
    }

    private sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id => "test-session";
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
    }

    private sealed class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
