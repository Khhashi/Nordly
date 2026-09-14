using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PG3302.Domain.Entities;
using PG3302.Domain.Services;

namespace OrderManager.Web.Pages.Orders;

public class DetailsModel : PageModel
{
    private readonly OrderService _service;

    public DetailsModel(OrderService service)
    {
        _service = service;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public string ProductName { get; set; } = string.Empty;

    [BindProperty]
    public decimal Price { get; set; }

    [BindProperty]
    public int Quantity { get; set; }

    public Order? Order { get; set; }

    public IActionResult OnGet()
    {
        Order = _service.GetOrder(Id);

        if (Order == null)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToPage("/Orders");
        }

        var line = Order.OrderLines.FirstOrDefault();
        ProductName = line?.Product.Name ?? string.Empty;
        Price = line?.Product.Price ?? 0;
        Quantity = line?.Quantity ?? 1;

        return Page();
    }

    public IActionResult OnPostUpdate()
    {
        if (string.IsNullOrWhiteSpace(ProductName) || Price <= 0 || Quantity <= 0)
        {
            TempData["Error"] = "Please enter a valid product name, price and quantity.";
            return RedirectToPage(new { id = Id });
        }

        try
        {
            var order = _service.GetOrder(Id);
            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToPage("/Orders");
            }

            order.OrderLines.Clear();
            order.AddProduct(new Product(ProductName.Trim(), Price), Quantity);
            _service.UpdateOrder(order);
            TempData["Success"] = "Order updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { id = Id });
    }

    public IActionResult OnPostDelete()
    {
        try
        {
            _service.DeleteOrder(Id);
            TempData["Success"] = "Order deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage("/Orders");
    }
}
