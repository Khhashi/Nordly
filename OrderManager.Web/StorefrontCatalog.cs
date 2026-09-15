namespace OrderManager.Web;

public sealed record StoreProduct(Guid Id, string Name, string Category, string Description, decimal Price, string ImageUrl)
{
    public static StoreProduct Create(string name, string category, string description, decimal price, string imageUrl) =>
        new(Guid.NewGuid(), name, category, description, price, imageUrl);
}

public static class StorefrontCatalog
{
    public static IReadOnlyList<StoreProduct> Products { get; } = new[]
    {
        StoreProduct.Create("Alba ceramic cup", "Kitchen", "A hand-finished cup for slow mornings and long tables.", 249, "https://images.unsplash.com/photo-1514228742587-6b1558fcca3d?auto=format&fit=crop&w=900&q=85"),
        StoreProduct.Create("Linen daily tote", "Carry", "A sturdy, relaxed tote made for everyday errands.", 399, "https://images.unsplash.com/photo-1594223274512-ad4803739b7c?auto=format&fit=crop&w=900&q=85"),
        StoreProduct.Create("Onda glass bottle", "Objects", "A clean glass bottle with a softly sculpted silhouette.", 299, "https://images.unsplash.com/photo-1602143407151-7111542de6e8?auto=format&fit=crop&w=900&q=85"),
        StoreProduct.Create("Noma desk tray", "Workspace", "Keep the small things together with warm natural oak.", 549, "https://images.unsplash.com/photo-1494438639946-1ebd1d20bf85?auto=format&fit=crop&w=900&q=85"),
        StoreProduct.Create("Kiyo incense holder", "Objects", "A quiet ceramic detail for the end of a long day.", 199, "https://images.unsplash.com/photo-1603006905003-be475563bc59?auto=format&fit=crop&w=900&q=85"),
        StoreProduct.Create("Forma wool throw", "Home", "Soft texture and considered warmth for the sofa.", 899, "https://images.unsplash.com/photo-1584100936595-c0654b55a2e2?auto=format&fit=crop&w=900&q=85"),
        StoreProduct.Create("Mori notebook", "Workspace", "A tactile notebook for lists, sketches and loose ideas.", 179, "https://images.unsplash.com/photo-1531346878377-a5be20888e57?auto=format&fit=crop&w=900&q=85"),
        StoreProduct.Create("Sora candle", "Home", "Warm cedar, soft smoke and a small moment of stillness.", 329, "https://images.unsplash.com/photo-1513506003901-1e6a229e2d15?auto=format&fit=crop&w=900&q=85")
    };
}