using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nordly.Web;

public sealed record PageMetadata(
    string Title,
    string Description,
    string CanonicalUrl,
    string ImageUrl,
    string Type = "website",
    decimal? Price = null);

public static class PageMetadataBuilder
{
    public const string DefaultPublicBaseUrl = "https://ordermanager-8ym2.onrender.com";
    public const string DefaultImageUrl = "https://images.unsplash.com/photo-1514228742587-6b1558fcca3d?auto=format&fit=crop&w=1200&q=85";
    public const int MaximumTitleLength = 70;
    public const int MaximumDescriptionLength = 200;

    public static PageMetadata CreateStorefront(string? publicBaseUrl, IReadOnlyList<StoreProduct> products, string? category, string? filter)
    {
        var categoryName = products.Select(product => product.Category)
            .FirstOrDefault(value => string.Equals(value, category, StringComparison.Ordinal));
        var isNewFilter = categoryName is null && filter == "new";
        var isSaleFilter = categoryName is null && filter == "sale";
        var title = categoryName is not null
            ? $"Kjøp {categoryName.ToLowerInvariant()} – NORDLY"
            : isNewFilter ? "Nyheter – NORDLY" : isSaleFilter ? "Tilbud – NORDLY" : "NORDLY – Norsk design til hverdagen";
        var description = categoryName is not null
            ? $"Utforsk NORDLYs produkter til {categoryName.ToLowerInvariant()}. Praktisk norsk design til hverdagen, valgt med omtanke."
            : isNewFilter ? "Se de nyeste produktene fra NORDLY – praktiske, vakre nyheter til hjemmet og hverdagen."
            : isSaleFilter ? "Se produkter på tilbud hos NORDLY. Finn gode hverdagsfavoritter til oppdaterte priser."
            : "Praktiske, vakre produkter som gjør hverdagen enklere. Norsk design med omtanke, små serier og rask levering.";
        var imageUrl = products.FirstOrDefault()?.ImageUrl ?? DefaultImageUrl;
        var query = BuildCategoryQuery(category, filter, products.Select(product => product.Category));

        return Create(publicBaseUrl, title, description, "/", imageUrl, query: query);
    }

    public static PageMetadata CreateProduct(string? publicBaseUrl, StoreProduct product) =>
        Create(
            publicBaseUrl,
            $"{product.Name} – kjøp hos NORDLY",
            product.Description,
            $"/Products/Details/{product.Id}",
            product.ImageUrl,
            "product",
            product.Price);

    public static PageMetadata Create(
        string? publicBaseUrl,
        string? title,
        string? description,
        string? path,
        string? imageUrl = null,
        string type = "website",
        decimal? price = null,
        string? query = null)
    {
        var safeTitle = LimitText(title, MaximumTitleLength, "NORDLY – Norsk design til hverdagen");
        var safeDescription = LimitText(description, MaximumDescriptionLength, "Praktiske, vakre produkter som gjør hverdagen enklere. Norsk design med omtanke.");

        return new PageMetadata(
            safeTitle,
            safeDescription,
            BuildCanonicalUrl(publicBaseUrl, path, query),
            IsAbsoluteHttpUrl(imageUrl) ? imageUrl! : DefaultImageUrl,
            type == "product" ? "product" : "website",
            price is >= 0m ? price : null);
    }

    public static string BuildCanonicalUrl(string? publicBaseUrl, string? path, string? query = null)
    {
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(baseUri.UserInfo)
            || !string.IsNullOrEmpty(baseUri.Query)
            || !string.IsNullOrEmpty(baseUri.Fragment))
        {
            baseUri = new Uri(DefaultPublicBaseUrl, UriKind.Absolute);
        }

        var safePath = string.IsNullOrWhiteSpace(path) || !path.StartsWith('/') || path.StartsWith("//", StringComparison.Ordinal)
            ? "/"
            : path;
        var result = new UriBuilder(baseUri)
        {
            Path = safePath,
            Query = query ?? string.Empty,
            Fragment = string.Empty
        };
        return result.Uri.AbsoluteUri;
    }

    public static string? BuildCategoryQuery(string? category, string? filter, IEnumerable<string> validCategories)
    {
        var validCategory = validCategories.FirstOrDefault(value => string.Equals(value, category, StringComparison.Ordinal));
        var validFilter = filter is "new" or "sale" ? filter : null;
        var parts = new List<string>();

        if (validCategory is not null && validCategory != "all")
            parts.Add($"category={Uri.EscapeDataString(validCategory)}");
        else if (validFilter is not null)
            parts.Add($"filter={Uri.EscapeDataString(validFilter)}");

        return parts.Count == 0 ? null : string.Join('&', parts);
    }

    public static IHtmlContent RenderTags(PageMetadata metadata)
    {
        var html = new HtmlContentBuilder();
        AddMeta(html, "name", "description", metadata.Description);
        AddLink(html, "canonical", metadata.CanonicalUrl);
        AddMeta(html, "property", "og:type", metadata.Type);
        AddMeta(html, "property", "og:site_name", "NORDLY");
        AddMeta(html, "property", "og:title", metadata.Title);
        AddMeta(html, "property", "og:description", metadata.Description);
        AddMeta(html, "property", "og:url", metadata.CanonicalUrl);
        AddMeta(html, "property", "og:image", metadata.ImageUrl);
        AddMeta(html, "property", "og:image:alt", metadata.Title);
        AddMeta(html, "name", "twitter:card", "summary_large_image");
        AddMeta(html, "name", "twitter:title", metadata.Title);
        AddMeta(html, "name", "twitter:description", metadata.Description);
        AddMeta(html, "name", "twitter:image", metadata.ImageUrl);

        if (metadata.Type == "product" && metadata.Price is decimal price)
        {
            AddMeta(html, "property", "product:price:amount", price.ToString("0.00", CultureInfo.InvariantCulture));
            AddMeta(html, "property", "product:price:currency", "NOK");
        }

        return html;
    }

    private static string LimitText(string? value, int maximumLength, string fallback)
    {
        var normalized = string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length == 0)
            return fallback;
        if (normalized.Length <= maximumLength)
            return normalized;

        var truncated = normalized[..(maximumLength - 1)];
        var lastSpace = truncated.LastIndexOf(' ');
        return (lastSpace > maximumLength / 2 ? truncated[..lastSpace] : truncated).TrimEnd(' ', '.', ',', ';', ':', '–', '—') + "…";
    }

    private static bool IsAbsoluteHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && string.IsNullOrEmpty(uri.UserInfo);

    private static void AddMeta(HtmlContentBuilder html, string keyAttribute, string key, string value)
    {
        var tag = new TagBuilder("meta");
        tag.TagRenderMode = TagRenderMode.SelfClosing;
        tag.Attributes[keyAttribute] = key;
        tag.Attributes["content"] = value;
        html.AppendHtml(tag);
    }

    private static void AddLink(HtmlContentBuilder html, string rel, string href)
    {
        var tag = new TagBuilder("link");
        tag.TagRenderMode = TagRenderMode.SelfClosing;
        tag.Attributes["rel"] = rel;
        tag.Attributes["href"] = href;
        html.AppendHtml(tag);
    }
}
