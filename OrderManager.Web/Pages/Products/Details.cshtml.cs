using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OrderManager.Web.Pages.Products;

public class DetailsModel : PageModel
{
    public StoreProduct? Product { get; private set; }

    public IActionResult OnGet(Guid id)
    {
        Product = StorefrontCatalog.Products.FirstOrDefault(product => product.Id == id);
        return Page();
    }

    public IActionResult OnPostAddToCart(Guid id, int quantity)
    {
        Product = StorefrontCatalog.Products.FirstOrDefault(product => product.Id == id);
        if (Product == null)
            return NotFound();

        var cartJson = HttpContext.Session.GetString("cart");
        var cart = string.IsNullOrWhiteSpace(cartJson)
            ? new List<CartItem>()
            : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        var item = cart.FirstOrDefault(cartItem => cartItem.ProductId == id);

        if (item == null)
            cart.Add(new CartItem(id, Math.Clamp(quantity, 1, 10)));
        else
            item.Quantity = Math.Clamp(item.Quantity + quantity, 1, 10);

        HttpContext.Session.SetString("cart", JsonSerializer.Serialize(cart));
        TempData["Success"] = $"{Product.Name} er lagt i handlekurven";
        return RedirectToPage(new { id });
    }
}