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
