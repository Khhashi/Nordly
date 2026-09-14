using PG3302.Domain.Entities;
using PG3302.Domain.Services;
using PG3302.Infrastructure.Repositories;

namespace PG3302;

class Program
{
    static void Main(string[] args)
    {
        var repository = new OrderRepository();
        var service = new OrderService(repository);

        bool running = true;

        while (running)
        {
            Console.WriteLine("\n=== ORDER MANAGEMENT SYSTEM ===");
            Console.WriteLine("1. Create order");
            Console.WriteLine("2. View order by ID");
            Console.WriteLine("3. List all orders");
            Console.WriteLine("4. Update order");
            Console.WriteLine("5. Delete order");
            Console.WriteLine("0. Exit");
            Console.Write("Choose option: ");

            var input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    CreateOrder(service);
                    break;

                case "2":
                    ViewOrder(service);
                    break;

                case "3":
                    ListOrders(service);
                    break;

                case "4":
                    UpdateOrder(service);
                    break;

                case "5":
                    DeleteOrder(service);
                    break;

                case "0":
                    running = false;
                    break;

                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    static void CreateOrder(OrderService service)
    {
        try
        {
            var order = new Order();

            Console.Write("Product name: ");
            var name = Console.ReadLine();

            Console.Write("Price: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal price))
            {
                Console.WriteLine("Invalid price.");
                return;
            }

            Console.Write("Quantity: ");
            if (!int.TryParse(Console.ReadLine(), out int quantity))
            {
                Console.WriteLine("Invalid quantity.");
                return;
            }

            var product = new Product(name!, price);
            order.AddProduct(product, quantity);

            service.CreateOrder(order);

            Console.WriteLine($"Order created. ID: {order.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void ViewOrder(OrderService service)
    {
        Console.Write("Enter Order ID: ");
        var idInput = Console.ReadLine();

        if (!Guid.TryParse(idInput, out Guid id))
        {
            Console.WriteLine("Invalid GUID format.");
            return;
        }

        var order = service.GetOrder(id);

        if (order == null)
        {
            Console.WriteLine("Order not found.");
            return;
        }

        Console.WriteLine($"\nOrder ID: {order.Id}");

        if (!order.HasProducts())
        {
            Console.WriteLine("(empty order)");
            return;
        }

        foreach (var line in order.OrderLines)
        {
            Console.WriteLine($"Product: {line.Product.Name}");
            Console.WriteLine($"Price: {line.Product.Price}");
            Console.WriteLine($"Quantity: {line.Quantity}");
            Console.WriteLine($"Line Total: {line.Product.Price * line.Quantity}");
            Console.WriteLine();
        }

        Console.WriteLine($"Order Total: {order.GetTotal()}");
    }

    static void ListOrders(OrderService service)
    {
        var orders = service.GetAllOrders();

        if (!orders.Any())
        {
            Console.WriteLine("No orders found.");
            return;
        }

        foreach (var order in orders)
        {
            if (order.HasProducts())
                Console.WriteLine($"ID: {order.Id} | Total: {order.GetTotal()}");
            else
                Console.WriteLine($"ID: {order.Id} | Total: (empty order)");
        }
    }

    static void UpdateOrder(OrderService service)
    {
        Console.Write("Enter Order ID to update: ");
        var idInput = Console.ReadLine();

        if (!Guid.TryParse(idInput, out Guid id))
        {
            Console.WriteLine("Invalid GUID format.");
            return;
        }

        var order = service.GetOrder(id);

        if (order == null)
        {
            Console.WriteLine("Order not found.");
            return;
        }

        try
        {
            Console.Write("New product name: ");
            var name = Console.ReadLine();

            Console.Write("Price: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal price))
            {
                Console.WriteLine("Invalid price.");
                return;
            }

            Console.Write("Quantity: ");
            if (!int.TryParse(Console.ReadLine(), out int quantity))
            {
                Console.WriteLine("Invalid quantity.");
                return;
            }

            var product = new Product(name!, price);
            order.AddProduct(product, quantity);

            service.UpdateOrder(order);

            Console.WriteLine("Order updated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void DeleteOrder(OrderService service)
    {
        Console.Write("Enter Order ID to delete: ");
        var idInput = Console.ReadLine();

        if (!Guid.TryParse(idInput, out Guid id))
        {
            Console.WriteLine("Invalid GUID format.");
            return;
        }

        try
        {
            service.DeleteOrder(id);
            Console.WriteLine("Order deleted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}