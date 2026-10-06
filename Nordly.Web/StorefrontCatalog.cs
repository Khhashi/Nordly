using System.Security.Cryptography;
using System.Text;

namespace Nordly.Web;

public sealed record StoreProduct(Guid Id, string Name, string Category, string Description, decimal Price, string ImageUrl, string? Badge = null, decimal? OriginalPrice = null, IReadOnlyList<string>? AdditionalImageUrls = null)
{
    public IReadOnlyList<string> ImageUrls => new[] { ImageUrl }
        .Concat(AdditionalImageUrls ?? Array.Empty<string>())
        .Where(url => !string.IsNullOrWhiteSpace(url))
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    public static StoreProduct Create(string name, string category, string description, decimal price, string imageUrl, string? badge = null, decimal? originalPrice = null, IReadOnlyList<string>? additionalImageUrls = null) =>
        new(CreateStableId(name), name, category, description, price, imageUrl, badge, originalPrice, additionalImageUrls?.ToArray());

    // Samme navn gir alltid samme ID, også etter at appen har startet på nytt.
    // Stripe-økten lagrer produkt-ID-ene, så de må være stabile mellom betaling og bekreftelse.
    private static Guid CreateStableId(string name) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes(name)));
}

public static class StorefrontCatalog
{
    public static IReadOnlyList<StoreProduct> Products { get; } = new[]
    {
        StoreProduct.Create("Alba keramikk kopp", "Kjøkken", "En håndlagd kopp til rolige morgener og lange måltider.", 249, "https://images.unsplash.com/photo-1514228742587-6b1558fcca3d?auto=format&fit=crop&w=900&q=85", "Bestselger"),
        StoreProduct.Create("Linen hverdagstaske", "Tilbehør", "En robust og avslappet taske til hverdagens små plikter.", 399, "https://images.unsplash.com/photo-1594223274512-ad4803739b7c?auto=format&fit=crop&w=900&q=85", "Ny"),
        StoreProduct.Create("Onda glassflaske", "Objekter", "En ren glassflaske med en myk, skulpturell form.", 299, "https://images.unsplash.com/photo-1602143407151-7111542de6e8?auto=format&fit=crop&w=900&q=85"),
        StoreProduct.Create("Noma skrivebordsbrett", "Kontor", "Hold de små tingene samlet med varmt naturtre.", 549, "https://images.unsplash.com/photo-1494438639946-1ebd1d20bf85?auto=format&fit=crop&w=900&q=85", "Begrenset"),
        StoreProduct.Create("Kiyo røkelsesholder", "Objekter", "Et rolig keramisk detaljobjekt til siste del av dagen.", 199, "https://images.unsplash.com/photo-1603006905003-be475563bc59?auto=format&fit=crop&w=900&q=85"),
        StoreProduct.Create("Forma ullteppe", "Hjem", "Myk tekstur og varme til sofaen.", 699, "https://images.unsplash.com/photo-1584100936595-c0654b55a2e2?auto=format&fit=crop&w=900&q=85", "-20%", 899),
        StoreProduct.Create("Mori notatbok", "Kontor", "En taktil notatbok for lister, skisser og løse idéer.", 179, "https://images.unsplash.com/photo-1531346878377-a5be20888e57?auto=format&fit=crop&w=900&q=85", "Ny"),
        StoreProduct.Create("Sora lykt", "Hjem", "Et varmt, dempet lys til lange kvelder.", 329, "https://images.unsplash.com/photo-1513506003901-1e6a229e2d15?auto=format&fit=crop&w=900&q=85", "Bestselger"),
        StoreProduct.Create("Milo reisekrus", "Kjøkken", "Et balansert, lekkasjekontrollert krus for morgenen på farten.", 349, "https://images.unsplash.com/photo-1577937927133-66ef06acdf18?auto=format&fit=crop&w=900&q=85"),
        StoreProduct.Create("Arco leselampe", "Kontor", "Et fokusert lys for sider, skisser og sen arbeidstid.", 799, "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?auto=format&fit=crop&w=900&q=85", "Ny"),
        StoreProduct.Create("Casa linputt", "Hjem", "Vasket lintekstur i en rolig, hverdagsskapende form.", 299, "https://images.unsplash.com/photo-1587433701752-78cbf88ae429?auto=format&fit=crop&w=900&q=85", "-15%", 349),
        StoreProduct.Create("Raku oppbevaringsboks", "Objekter", "En liten eikeboks for det som er verdt å ha nærme.", 449, "https://images.unsplash.com/photo-1618220179428-22790b461013?auto=format&fit=crop&w=900&q=85")
    };

    public static IReadOnlyList<StoreProduct> RelatedProductsFor(Guid productId, int maximumCount = 4)
    {
        if (maximumCount <= 0)
            return Array.Empty<StoreProduct>();

        var product = Products.FirstOrDefault(candidate => candidate.Id == productId);
        if (product is null)
            return Array.Empty<StoreProduct>();

        return Products
            .Where(candidate => candidate.Id != product.Id && candidate.Category == product.Category)
            .Take(maximumCount)
            .ToArray();
    }
}