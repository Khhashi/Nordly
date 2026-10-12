using System.Text.Encodings.Web;
using Nordly.Web;
using NUnit.Framework;

namespace Nordly.Tests;

public class PageMetadataTests
{
    private const string PublicBaseUrl = "https://butikk.nordly.no";

    [Test]
    public void Homepage_Metadata_Renders_Description_Canonical_And_Social_Cards()
    {
        var metadata = PageMetadataBuilder.CreateStorefront(PublicBaseUrl, StorefrontCatalog.Products, null, null);

        var html = Render(metadata);

        AssertMeta(html, "name", "description", "Praktiske, vakre produkter som gjør hverdagen enklere. Norsk design med omtanke, små serier og rask levering.");
        Assert.That(html, Does.Contain("rel=\"canonical\""));
        Assert.That(html, Does.Contain("href=\"https://butikk.nordly.no/\""));
        AssertMeta(html, "property", "og:title", "NORDLY – Norsk design til hverdagen");
        AssertMeta(html, "name", "twitter:card", "summary_large_image");
        Assert.That(html, Does.Not.Contain("<h1"));
    }

    [Test]
    public void Product_Metadata_Uses_Product_Name_Description_Image_And_Nok_Price()
    {
        var product = StorefrontCatalog.Products.Single(item => item.Name == "Forma ullteppe");
        var metadata = PageMetadataBuilder.CreateProduct(PublicBaseUrl, product);

        var html = Render(metadata);

        AssertMeta(html, "property", "og:title", $"{product.Name} – kjøp hos NORDLY");
        AssertMeta(html, "property", "og:description", product.Description);
        AssertMeta(html, "property", "og:image", product.ImageUrl);
        Assert.That(html, Does.Contain($"https://butikk.nordly.no/Products/Details/{product.Id}"));
        AssertMeta(html, "property", "product:price:amount", "699.00");
        AssertMeta(html, "property", "product:price:currency", "NOK");
    }

    [Test]
    public void Information_Page_Metadata_Renders_Its_Own_Text_And_Canonical_Url()
    {
        var metadata = PageMetadataBuilder.Create(
            PublicBaseUrl,
            "Levering og retur – NORDLY",
            "Les om leveringstid, fraktkostnader og retur hos NORDLY.",
            "/Info/Delivery");

        var html = Render(metadata);

        AssertMeta(html, "property", "og:title", "Levering og retur – NORDLY");
        AssertMeta(html, "property", "og:description", "Les om leveringstid, fraktkostnader og retur hos NORDLY.");
        Assert.That(html, Does.Contain("https://butikk.nordly.no/Info/Delivery"));
        Assert.That(html, Does.Not.Contain("product:price"));
    }

    [Test]
    public void Category_Filter_Metadata_Has_Canonical_Filter_Url_And_Ignores_Unknown_Values()
    {
        var metadata = PageMetadataBuilder.CreateStorefront(PublicBaseUrl, StorefrontCatalog.Products, "Kjøkken", "sale");
        var html = Render(metadata);
        var categories = StorefrontCatalog.Products.Select(product => product.Category).Distinct();
        var invalidQuery = PageMetadataBuilder.BuildCategoryQuery("../../evil", "unknown", categories);

        AssertMeta(html, "property", "og:title", "Kjøp kjøkken – NORDLY");
        Assert.That(html, Does.Contain("https://butikk.nordly.no/?category=Kj%C3%B8kken"));
        Assert.That(invalidQuery, Is.Null);
    }

    [Test]
    public void Metadata_Is_Truncated_Uses_Fallbacks_And_Encodes_Untrusted_Text()
    {
        var metadata = PageMetadataBuilder.Create(
            "http://attacker.example",
            "<script>alert(1)</script>",
            new string('x', PageMetadataBuilder.MaximumDescriptionLength + 20),
            "//attacker.example/phishing",
            "javascript:alert(1)\" onerror=\"alert(1)");

        var html = Render(metadata);

        Assert.That(metadata.CanonicalUrl, Is.EqualTo("https://ordermanager-8ym2.onrender.com/"));
        Assert.That(metadata.ImageUrl, Is.EqualTo(PageMetadataBuilder.DefaultImageUrl));
        Assert.That(metadata.Title, Is.EqualTo("<script>alert(1)</script>"));
        Assert.That(metadata.Description.Length, Is.LessThanOrEqualTo(PageMetadataBuilder.MaximumDescriptionLength));
        Assert.That(html, Does.Not.Contain("attacker.example"));
        Assert.That(html, Does.Not.Contain("javascript:"));
        Assert.That(html, Does.Contain("&lt;script&gt;"));
    }

    private static string Render(PageMetadata metadata)
    {
        using var writer = new StringWriter();
        PageMetadataBuilder.RenderTags(metadata).WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    private static void AssertMeta(string html, string keyAttribute, string key, string content)
    {
        Assert.That(html, Does.Contain($"{keyAttribute}=\"{key}\""));
        Assert.That(html, Does.Contain($"content=\"{HtmlEncoder.Default.Encode(content)}\""));
    }
}
