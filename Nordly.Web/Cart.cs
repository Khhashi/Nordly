using System.Text.Json;

namespace Nordly.Web;

/// <summary>
/// Handlekurven lagret i sesjonen. All logikk for å lese, endre og lagre kurven ligger her.
/// </summary>
public sealed class Cart
{
    public const int MaxQuantityPerProduct = 10;
    private const string SessionKey = "cart";

    private readonly List<CartItem> _items;

    private Cart(List<CartItem> items) => _items = items;

    public IReadOnlyList<CartItem> Items => _items;
    public int Count => _items.Sum(item => item.Quantity);

    public static Cart Empty() => new(new List<CartItem>());

    public static Cart Load(ISession session)
    {
        var json = session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(json))
            return Empty();

        try
        {
            return new Cart(JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>());
        }
        catch (JsonException)
        {
            return Empty();
        }
    }

    public void Save(ISession session) => session.SetString(SessionKey, JsonSerializer.Serialize(_items));

    public static void Clear(ISession session) => session.Remove(SessionKey);

    public int QuantityOf(Guid productId) =>
        _items.FirstOrDefault(item => item.ProductId == productId)?.Quantity ?? 0;

    public void Add(Guid productId, int quantity = 1)
    {
        if (quantity <= 0)
            return;

        var item = _items.FirstOrDefault(cartItem => cartItem.ProductId == productId);
        if (item == null)
            _items.Add(new CartItem(productId, Math.Min(quantity, MaxQuantityPerProduct)));
        else
            item.Quantity = Math.Min(item.Quantity + quantity, MaxQuantityPerProduct);
    }

    public void Decrease(Guid productId)
    {
        var item = _items.FirstOrDefault(cartItem => cartItem.ProductId == productId);
        if (item == null)
            return;

        item.Quantity--;
        if (item.Quantity <= 0)
            _items.Remove(item);
    }

    public void Remove(Guid productId) => _items.RemoveAll(item => item.ProductId == productId);
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
