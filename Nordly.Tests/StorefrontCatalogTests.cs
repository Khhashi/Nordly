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
}
