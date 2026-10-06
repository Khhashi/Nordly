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

    [Test]
    public void Related_Products_Should_Only_Include_Other_Products_In_The_Same_Category()
    {
        var product = StorefrontCatalog.Products.First(item => item.Category == "Kjøkken");

        var related = StorefrontCatalog.RelatedProductsFor(product.Id);

        Assert.That(related, Is.Not.Empty);
        Assert.That(related, Has.None.Matches<StoreProduct>(item => item.Id == product.Id));
        Assert.That(related, Has.All.Matches<StoreProduct>(item => item.Category == product.Category));
    }

    [Test]
    public void Related_Products_Should_Respect_The_Maximum_Count()
    {
        var product = StorefrontCatalog.Products.First(item => item.Category == "Objekter");

        var related = StorefrontCatalog.RelatedProductsFor(product.Id, 1);

        Assert.That(related, Has.Count.EqualTo(1));
    }

    [Test]
    public void Related_Products_Should_Be_Empty_For_Unknown_Product_Or_Nonpositive_Limit()
    {
        Assert.That(StorefrontCatalog.RelatedProductsFor(Guid.NewGuid()), Is.Empty);
        Assert.That(StorefrontCatalog.RelatedProductsFor(StorefrontCatalog.Products[0].Id, 0), Is.Empty);
    }

    [Test]
    public void Product_Specifications_Should_Be_Omitted_When_No_Details_Are_Provided()
    {
        var product = StoreProduct.Create("Produkt", "Kategori", "Beskrivelse", 100, "bilde.jpg");

        Assert.That(product.Specifications, Is.Null);
        Assert.That(new StoreProductSpecifications().HasDetails, Is.False);
        Assert.That(new StoreProductSpecifications(Highlights: new[] { "", " " }).HasDetails, Is.False);
    }

    [Test]
    public void Product_Specifications_Should_Be_Shown_When_A_Fact_Is_Provided()
    {
        var product = StorefrontCatalog.Products.Single(item => item.Name == "Alba keramikk kopp");

        Assert.That(product.Specifications?.HasDetails, Is.True);
        Assert.That(product.Specifications?.Material, Is.EqualTo("Keramikk"));
    }
}
