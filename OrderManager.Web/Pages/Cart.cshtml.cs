using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PG3302.Domain.Entities;
using PG3302.Domain.Services;

namespace OrderManager.Web.Pages;

public class CartModel : PageModel
{
    private readonly OrderService _service;

    public CartModel(OrderService service) => _service = service;

    public List<CartViewItem> Items { get; private set; } = new();
    public decimal Total => Items.Sum(item => item.Product.Price * item.Quantity);

    public void OnGet() => LoadCart();

    public IActionResult OnPostCheckout()
    {
        LoadCart();

        if (!Items.Any())
        {
            TempData["Error"] = "Handlekurven er tom.";
            return RedirectToPage();
        }

        var order = new Order();
        foreach (var item in Items)
            order.AddProduct(new Product(item.Product.Name, item.Product.Price), item.Quantity);

        _service.CreateOrder(order);
        HttpContext.Session.Remove("cart");
        TempData["Success"] = $"Takk for bestillingen! Ordren din er {order.Id.ToString()[..8].ToUpperInvariant()}.";
        return RedirectToPage("/Orders/Details", new { id = order.Id });
    }

    private void LoadCart()
    {
        var json = HttpContext.Session.GetString("cart");
        var cart = string.IsNullOrWhiteSpace(json)
            ? new List<CartItem>()
            : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();

        Items = cart
            .Select(item =>
            {
                var product = StorefrontCatalog.Products.FirstOrDefault(candidate => candidate.Id == item.ProductId);
                return product == null ? null : new CartViewItem(product, item.Quantity);
            })
            .Where(item => item != null)
            .Cast<CartViewItem>()
            .ToList();
    }
}

public record CartViewItem(StoreProduct Product, int Quantity);