using NUnit.Framework;
using Nordly.Web;

namespace Nordly.Tests;

public class StorefrontCatalogTests
{
    [Test]
    public void Same_Product_Name_Should_Always_Get_Same_Id()
    {
        var first = StoreProduct.Create("Alba keramikk kopp", "Kjøkken", "Beskrivelse", 249, "bilde.jpg");
        var second = StoreProduct.Create("Alba keramikk kopp", "Kjøkken", "Beskrivelse", 249, "bilde.jpg");

        Assert.That(first.Id, Is.EqualTo(second.Id));
    }

    [Test]
    public void All_Products_In_Catalog_Should_Have_Unique_Ids()
    {
        var ids = StorefrontCatalog.Products.Select(product => product.Id).ToList();

        Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
    }

    [Test]
    public void All_Products_In_Catalog_Should_Have_Unique_Images()
    {
        var images = StorefrontCatalog.Products.Select(product => product.ImageUrl).ToList();

        Assert.That(images.Distinct().Count(), Is.EqualTo(images.Count));
    }

    [Test]
    public void Product_Image_Urls_Should_Start_With_Primary_Image()
    {
        var product = StoreProduct.Create("Produkt", "Kategori", "Beskrivelse", 100, "hovedbilde.jpg");

        Assert.That(product.ImageUrls, Is.EqualTo(new[] { "hovedbilde.jpg" }));
    }

    [Test]
    public void Product_Image_Urls_Should_Keep_Additional_Images_In_Order()
    {
        var product = StoreProduct.Create(
            "Produkt",
            "Kategori",
            "Beskrivelse",
            100,
            "hovedbilde.jpg",
            additionalImageUrls: new[] { "detalj.jpg", "bakside.jpg" });

        Assert.That(product.ImageUrls, Is.EqualTo(new[] { "hovedbilde.jpg", "detalj.jpg", "bakside.jpg" }));
    }

    [Test]
    public void Product_Image_Urls_Should_Remove_Blank_And_Duplicate_Urls()
    {
        var product = StoreProduct.Create(
            "Produkt",
            "Kategori",
            "Beskrivelse",
            100,
            "hovedbilde.jpg",
            additionalImageUrls: new[] { "hovedbilde.jpg", "", "detalj.jpg", "detalj.jpg" });

        Assert.That(product.ImageUrls, Is.EqualTo(new[] { "hovedbilde.jpg", "detalj.jpg" }));
    }
}
