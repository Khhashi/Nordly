using System.Text.Json;
using PG3302.Domain.Entities;
using PG3302.Domain.Interfaces;

namespace PG3302.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly string _filePath;
    private List<Order> _orders;

    public OrderRepository()
    {
        var runtimeDirectory = AppContext.BaseDirectory;
        var directory = new DirectoryInfo(runtimeDirectory);

        _filePath = Path.Combine(runtimeDirectory, "orders.json");

        if (!Directory.Exists(runtimeDirectory))
        {
            Directory.CreateDirectory(runtimeDirectory);
        }

        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);

            _orders = JsonSerializer.Deserialize<List<Order>>(json)
                      ?? new List<Order>();
        }
        else
        {
            _orders = new List<Order>();
            SaveToFile();
        }
    }

    public void Add(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        _orders.Add(order);
        SaveToFile();
    }

    public Order? GetById(Guid id)
    {
        return _orders.FirstOrDefault(o => o.Id == id);
    }

    public List<Order> GetAll()
    {
        return _orders;
    }

    public void Update(Order order)
    {
        var existing = GetById(order.Id);

        if (existing == null)
            throw new InvalidOperationException("Order not found.");

        _orders.Remove(existing);
        _orders.Add(order);

        SaveToFile();
    }

    public void Delete(Guid id)
    {
        var order = GetById(id);

        if (order == null)
            throw new InvalidOperationException("Order not found.");

        _orders.Remove(order);

        SaveToFile();
    }

    private void SaveToFile()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(_orders, options);
        File.WriteAllText(_filePath, json);
    }
}