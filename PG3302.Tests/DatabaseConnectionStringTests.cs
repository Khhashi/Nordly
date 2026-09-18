using NUnit.Framework;
using Nordly.Web;

namespace PG3302.Tests;

public class DatabaseConnectionStringTests
{
    [Test]
    public void Normalize_Should_Convert_Render_Postgres_Url()
    {
        var result = DatabaseConnectionString.Normalize(
            "postgres://render_user:secret%40value@dpg.example.com:5432/nordly");

        Assert.That(result, Does.Contain("Host=dpg.example.com"));
        Assert.That(result, Does.Contain("Port=5432"));
        Assert.That(result, Does.Contain("Database=nordly"));
        Assert.That(result, Does.Contain("Username=render_user"));
        Assert.That(result, Does.Contain("Password=secret@value"));
        Assert.That(result, Does.Contain("SSL Mode=Require"));
    }

    [Test]
    public void Normalize_Should_Keep_Npgsql_Connection_String()
    {
        const string connectionString = "Host=localhost;Port=5432;Database=nordly";

        Assert.That(DatabaseConnectionString.Normalize(connectionString), Is.EqualTo(connectionString));
    }

    [Test]
    public void Normalize_Should_Default_Render_Postgres_Port_To_5432()
    {
        var result = DatabaseConnectionString.Normalize("postgres://user:secret@db.example.com/nordly");

        Assert.That(result, Does.Contain("Port=5432"));
    }
}