using System.Security.Cryptography;
using System.Text;

namespace Nordly.Web;

public sealed record StoreProduct(Guid Id, string Name, string Category, string Description, decimal Price, string ImageUrl, string? Badge = null, decimal? OriginalPrice = null, IReadOnlyList<string>? AdditionalImageUrls = null, StoreProductSpecifications? Specifications = null)
{
    public decimal Savings => OriginalPrice is decimal originalPrice && originalPrice > Price
        ? originalPrice - Price
        : 0m;

    public int SavingsPercentage => OriginalPrice is > 0m && Savings > 0m
        ? (int)Math.Round(Savings / OriginalPrice.Value * 100m, MidpointRounding.AwayFromZero)
        : 0;

    public bool IsOnSale => Savings > 0m;

    public IReadOnlyList<string> ImageUrls => new[] { ImageUrl }
        .Concat(AdditionalImageUrls ?? Array.Empty<string>())
        .Where(url => !string.IsNullOrWhiteSpace(url))
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    public static StoreProduct Create(string name, string category, string description, decimal price, string imageUrl, string? badge = null, decimal? originalPrice = null, IReadOnlyList<string>? additionalImageUrls = null, StoreProductSpecifications? specifications = null) =>
        new(CreateStableId(name), name, category, description, price, imageUrl, badge, originalPrice, additionalImageUrls?.ToArray(), specifications);

    // Samme navn gir alltid samme ID, også etter at appen har startet på nytt.
    // Stripe-økten lagrer produkt-ID-ene, så de må være stabile mellom betaling og bekreftelse.
    private static Guid CreateStableId(string name) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes(name)));
}

public sealed record StoreProductSpecifications(
    string? Material = null,
    string? Dimensions = null,
    string? CareInstructions = null,
    IReadOnlyList<string>? Highlights = null)
{
    public bool HasDetails =>
        !string.IsNullOrWhiteSpace(Material)
        || !string.IsNullOrWhiteSpace(Dimensions)
        || !string.IsNullOrWhiteSpace(CareInstructions)
        || Highlights?.Any(highlight => !string.IsNullOrWhiteSpace(highlight)) == true;
}

public static class StorefrontCatalog
{
    public const string FallbackImageUrl = "/images/demo/product-placeholder.svg";

    public static IReadOnlyList<StoreProduct> Products { get; } = new[]
    {
        StoreProduct.Create("Alba keramikk kopp", "Kjøkken", "En håndlagd kopp til rolige morgener og lange måltider.", 249, "/images/products/alba-ceramic-mug.svg", "Bestselger", specifications: new(Material: "Keramikk", Highlights: new[] { "Håndlaget", "Til rolige morgener og lange måltider" })),
        StoreProduct.Create("Sora serveringsskål (demo)", "Kjøkken", "Illustrativt demoprodukt; ikke en faktisk vare for salg.", 279, "/images/demo/kitchen-bowl.svg", "Demo"),
        StoreProduct.Create("Mira kjøkkenhåndkle (demo)", "Kjøkken", "Illustrativt demoprodukt; ikke en faktisk vare for salg.", 149, "/images/demo/kitchen-towel.svg", "Demo"),
        StoreProduct.Create("Linen hverdagstaske", "Tilbehør", "En robust og avslappet taske til hverdagens små plikter.", 399, "/images/products/linen-everyday-bag.svg", "Ny", specifications: new(Highlights: new[] { "Robust til hverdagens små plikter", "Avslappet stil" })),
        StoreProduct.Create("Kompakt handlenett", "Tilbehør", "Et lett handlenett til ærender og ekstra plass på farten.", 249, "/images/products/compact-tote.svg", "Ny"),
        StoreProduct.Create("Flettet skulderveske", "Tilbehør", "En kompakt veske for de viktigste småtingene på farten.", 449, "/images/products/woven-shoulder-bag.svg"),
        StoreProduct.Create("Kompakt dagstursekk", "Tilbehør", "En kompakt sekk til korte turer og dager på farten.", 699, "/images/products/daypack.svg"),
        StoreProduct.Create("Onda glassflaske", "Objekter", "En ren glassflaske med en myk, skulpturell form.", 299, "/images/products/glass-bottle.svg", specifications: new(Material: "Glass", Highlights: new[] { "Myk, skulpturell form" })),
        StoreProduct.Create("Noma skrivebordsbrett", "Kontor", "Hold de små tingene samlet med varmt naturtre.", 549, "/images/products/desk-tray.svg", "Begrenset", specifications: new(Material: "Naturtre", Highlights: new[] { "Samler småting på arbeidsplassen" })),
        StoreProduct.Create("Kiyo røkelsesholder", "Objekter", "Et rolig keramisk detaljobjekt til siste del av dagen.", 199, "/images/products/incense-holder.svg"),
        StoreProduct.Create("Forma ullteppe", "Hjem", "Myk tekstur og varme til sofaen.", 699, "/images/products/wool-throw.svg", "-20%", 899, specifications: new(Material: "Ull", Highlights: new[] { "Myk tekstur", "Gir varme til sofaen" })),
        StoreProduct.Create("Mori notatbok", "Kontor", "En taktil notatbok for lister, skisser og løse idéer.", 179, "/images/products/notebook.svg", "Ny"),
        StoreProduct.Create("Sora lykt", "Hjem", "Et varmt, dempet lys til lange kvelder.", 329, "/images/products/lantern.svg", "Bestselger"),
        StoreProduct.Create("Milo reisekrus", "Kjøkken", "Et balansert, lekkasjekontrollert krus for morgenen på farten.", 349, "/images/products/travel-mug.svg"),
        StoreProduct.Create("Arco leselampe", "Kontor", "Et fokusert lys for sider, skisser og sen arbeidstid.", 799, "/images/products/reading-lamp.svg", "Ny"),
        StoreProduct.Create("Casa linputt", "Hjem", "Vasket lintekstur i en rolig, hverdagsskapende form.", 299, "/images/products/linen-cushion.svg", "-15%", 349),
        StoreProduct.Create("Raku oppbevaringsboks", "Objekter", "En liten eikeboks for det som er verdt å ha nærme.", 449, "/images/products/storage-box.svg")
    };

    public static IReadOnlyList<StoreProduct> RelatedProductsFor(Guid productId, int maximumCount = 4)
    {
        if (maximumCount <= 0)
            return Array.Empty<StoreProduct>();

        var product = Products.FirstOrDefault(candidate => candidate.Id == productId);
        if (product is null)
            return Array.Empty<StoreProduct>();

        var otherProducts = Products.Where(candidate => candidate.Id != product.Id);
        var sameCategory = otherProducts.Where(candidate => candidate.Category == product.Category);
        var otherCategories = otherProducts.Where(candidate => candidate.Category != product.Category);

        return sameCategory
            .Concat(otherCategories)
            .Take(maximumCount)
            .ToArray();
    }
}