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
        StoreProduct.Create("Noma desk tray", "Workspace", "Keep the small things together with warm natural oak.", 549, "https://images.unsplash.com/photo-1494438639946-1ebd1d20bf85?auto=format&fit=crop&w=900&q=85")
    };
}