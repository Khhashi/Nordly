using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using PG3302.Domain.Entities;
using PG3302.Domain.Services;

namespace OrderManager.Web.Pages;

public class IndexModel : PageModel
{
    private readonly OrderService _service;

    public IndexModel(OrderService service)
    {
        _service = service;
    }

    public IReadOnlyList<StoreProduct> Products => StorefrontCatalog.Products;
    public int CartCount { get; private set; }

    public void OnGet()
    {
        CartCount = GetCart().Sum(item => item.Quantity);
    }

    public IActionResult OnPostAddToCart(Guid productId)
    {
        if (!StorefrontCatalog.Products.Any(product => product.Id == productId))
        {
            return NotFound();
        }

        var cart = GetCart();
        var item = cart.FirstOrDefault(cartItem => cartItem.ProductId == productId);

        if (item == null)
            cart.Add(new CartItem(productId, 1));
        else
            item.Quantity++;

        SaveCart(cart);
        TempData["Success"] = "Produktet er lagt i handlekurven.";
        return RedirectToPage();
    }

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString("cart");
        return string.IsNullOrWhiteSpace(json)
            ? new List<CartItem>()
            : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
    }

    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetString("cart", JsonSerializer.Serialize(cart));
    }
}

public sealed class CartItem
{
    public CartItem(Guid productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }

    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
