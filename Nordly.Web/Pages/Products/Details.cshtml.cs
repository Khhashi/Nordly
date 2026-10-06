using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Nordly.Web.Pages.Products;

public class DetailsModel : PageModel
{
    public StoreProduct? Product { get; private set; }
    public IReadOnlyList<StoreProduct> RelatedProducts => Product is null
        ? Array.Empty<StoreProduct>()
        : StorefrontCatalog.RelatedProductsFor(Product.Id);

    public IActionResult OnGet(Guid id)
    {
        Product = StorefrontCatalog.Products.FirstOrDefault(product => product.Id == id);
        if (Product == null)
            Response.StatusCode = StatusCodes.Status404NotFound;
        return Page();
    }

    public IActionResult OnPostAddToCart(Guid id, int quantity)
    {
        Product = StorefrontCatalog.Products.FirstOrDefault(product => product.Id == id);
        if (Product == null)
            return NotFound();

        var cart = Cart.Load(HttpContext.Session);
        cart.Add(id, Math.Max(quantity, 1));
        cart.Save(HttpContext.Session);

        TempData["Success"] = $"{Product.Name} er lagt i handlekurven";
        return RedirectToPage(new { id });
    }
}
