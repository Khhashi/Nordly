using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PG3302.Domain.Entities;
using PG3302.Domain.Services;

namespace OrderManager.Web.Pages;

public class OrdersModel : PageModel
{
    private readonly OrderService _service;

    public OrdersModel(OrderService service)
    {
        _service = service;
    }

    [BindProperty]
    public string ProductName { get; set; } = string.Empty;

    [BindProperty]
    public decimal Price { get; set; }

    [BindProperty]
    public int Quantity { get; set; }

    public List<Order> Orders { get; set; } = new();

    public void OnGet()
    {
        Orders = _service.GetAllOrders();
    }

    public IActionResult OnPostCreate()
    {
        if (string.IsNullOrWhiteSpace(ProductName) || Price <= 0 || Quantity <= 0)
        {
            TempData["Error"] = "Please enter a valid product name, price and quantity.";
            return RedirectToPage();
        }

        var order = new Order();
        var product = new Product(ProductName.Trim(), Price);
        order.AddProduct(product, Quantity);

        _service.CreateOrder(order);
        TempData["Success"] = "Order created successfully.";

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(Guid orderId)
    {
        try
        {
            _service.DeleteOrder(orderId);
            TempData["Success"] = "Order deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }

}
