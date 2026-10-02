using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Nordly.Infrastructure.Data;

namespace Nordly.Tests;

public class DataProtectionKeyTests
{
    private static ServiceProvider CreateApp(InMemoryDatabaseRoot root, string databaseName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<OrderDbContext>(options => options.UseInMemoryDatabase(databaseName, root));
        services.AddDataProtection()
            .PersistKeysToDbContext<OrderDbContext>()
            .SetApplicationName("Nordly");
        return services.BuildServiceProvider();
    }

    [Test]
    public void Keys_Are_Stored_In_Database_And_Survive_A_Restart()
    {
        var root = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();

        string token;
        using (var firstApp = CreateApp(root, databaseName))
        {
            token = firstApp.GetDataProtector("test").Protect("handlekurv");

            using var scope = firstApp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            Assert.That(db.DataProtectionKeys.Count(), Is.GreaterThan(0));
        }

        using var restartedApp = CreateApp(root, databaseName);
        Assert.That(restartedApp.GetDataProtector("test").Unprotect(token), Is.EqualTo("handlekurv"));
    }
}
