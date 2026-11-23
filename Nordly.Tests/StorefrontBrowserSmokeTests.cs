using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using Nordly.Infrastructure.Data;
using Nordly.Web;

namespace Nordly.Tests;

[TestFixture]
[NonParallelizable]
public sealed class StorefrontBrowserSmokeTests : PageTest
{
    private Process? _webProcess;
    private string _baseUrl = string.Empty;
    private readonly StringBuilder _serverOutput = new();
    private readonly object _serverOutputLock = new();

    [OneTimeSetUp]
    public async Task StartStorefrontAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("NORDLY_BROWSER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Ignore("Set NORDLY_BROWSER_TEST_CONNECTION to run browser smoke tests against a PostgreSQL test database.");

        await MigrateTestDatabaseAsync(connectionString);

        var repositoryRoot = FindRepositoryRoot();
        var configuration = Directory.GetParent(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory))?.Name
            ?? throw new InvalidOperationException("Could not determine the test build configuration.");
        var webProjectDirectory = Path.Combine(repositoryRoot, "Nordly.Web");
        var webAssemblyPath = Path.Combine(webProjectDirectory, "bin", configuration, "net8.0", "Nordly.Web.dll");
        if (!File.Exists(webAssemblyPath))
            throw new FileNotFoundException("Build Nordly.Web before running browser smoke tests.", webAssemblyPath);

        var port = GetAvailablePort();
        var applicationUrl = $"http://127.0.0.1:{port}";
        _baseUrl = applicationUrl;
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = webProjectDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add(webAssemblyPath);
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(applicationUrl);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        startInfo.Environment["ConnectionStrings__DefaultConnection"] = connectionString;
        startInfo.Environment["Stripe__SecretKey"] = string.Empty;
        startInfo.Environment["Stripe__WebhookSecret"] = string.Empty;
        startInfo.Environment["Email__BrevoApiKey"] = string.Empty;
        startInfo.Environment["Email__FromAddress"] = string.Empty;

        _webProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _webProcess.OutputDataReceived += (_, eventArgs) => AppendServerOutput(eventArgs.Data);
        _webProcess.ErrorDataReceived += (_, eventArgs) => AppendServerOutput(eventArgs.Data);
        if (!_webProcess.Start())
            throw new InvalidOperationException("Could not start the Nordly web application for browser smoke tests.");

        _webProcess.BeginOutputReadLine();
        _webProcess.BeginErrorReadLine();
        await WaitForApplicationAsync(applicationUrl, _webProcess);
    }

    [OneTimeTearDown]
    public async Task StopStorefrontAsync()
    {
        if (_webProcess is not null)
        {
            if (!_webProcess.HasExited)
            {
                _webProcess.Kill(entireProcessTree: true);
                await _webProcess.WaitForExitAsync();
            }

            _webProcess.Dispose();
        }
    }

    [SetUp]
    public async Task BlockExternalRequestsAsync()
    {
        await Page.RouteAsync("https://**/*", route => route.AbortAsync());
    }

    [Test]
    public async Task Search_And_Category_Filter_Show_Only_Matching_Products()
    {
        await Page.GotoAsync(_baseUrl);
        var search = Page.Locator("#product-search");
        await search.FillAsync("Mori");
        await Expect(Page.Locator("#collection .product-card:visible")).ToHaveCountAsync(1);
        await Page.Locator("button[data-category='Kjøkken']").ClickAsync();

        Assert.That(await Page.Locator("#collection .product-card:visible").CountAsync(), Is.Zero);
        Assert.That(await Page.Locator(".no-results").IsVisibleAsync(), Is.True);

        await Page.Locator("button[data-category='Kontor']").ClickAsync();
        var visibleProducts = Page.Locator("#collection .product-card:visible");
        await Expect(visibleProducts).ToHaveCountAsync(1);
        Assert.That(await visibleProducts.InnerTextAsync(), Does.Contain("Mori notatbok"));
    }

    [Test]
    public async Task Broken_Product_Image_Uses_Local_Fallback_And_Keeps_Alt_Text()
    {
        var product = StorefrontCatalog.Products.First();
        await Page.RouteAsync($"**{product.ImageUrl}", route => route.AbortAsync());
        await Page.GotoAsync(_baseUrl);

        var image = Page.Locator(".hero-visual img");
        await Expect(image).ToHaveAttributeAsync("src", StorefrontCatalog.FallbackImageUrl);
        await Expect(image).ToHaveAttributeAsync("alt", product.Name);
        Assert.That(await image.EvaluateAsync<int>("image => image.naturalWidth"), Is.GreaterThan(0));
    }

    [Test]
    public async Task Shipping_And_Return_Copy_Explains_That_The_Storefront_Is_A_Demo()
    {
        await Page.GotoAsync(_baseUrl);
        await Expect(Page.Locator(".service-grid")).ToContainTextAsync("Ingen varer selges, sendes eller tas i retur.");

        await Page.GotoAsync($"{_baseUrl}/Info/Delivery");
        await Expect(Page.Locator(".info-lead")).ToContainTextAsync("Dette er en demo.");

        await Page.GotoAsync($"{_baseUrl}/Info/Faq");
        await Expect(Page.Locator(".info-lead")).ToContainTextAsync("NORDLY er en demo.");
    }

    [Test]
    public async Task Adding_Product_Updates_Cart_And_Persists_Across_Pages()
    {
        await Page.GotoAsync(_baseUrl);
        var product = Page.Locator("#collection .product-card")
            .Filter(new LocatorFilterOptions { HasText = "Alba keramikk kopp" });
        await product.GetByRole(AriaRole.Button, new() { Name = "Legg én Alba keramikk kopp i handlekurven" }).ClickAsync();
        await Expect(product.Locator(".quantity-value")).ToHaveTextAsync("1");
        await Expect(Page.Locator(".nav-badge").First).ToHaveTextAsync("1");

        await Page.Locator(".nav-links a[href='/Cart']").ClickAsync();
        await Expect(Page.Locator(".cart-item h2")).ToHaveTextAsync("Alba keramikk kopp");
        await Expect(Page.Locator(".quantity-stepper > span")).ToHaveTextAsync("1");
    }

    [Test]
    public async Task Cancelled_Checkout_Return_Snapshot_Restores_The_Cart()
    {
        var product = StorefrontCatalog.Products.Single(item => item.Name == "Mori notatbok");
        var snapshot = Uri.EscapeDataString($"{product.Id:N}:2");

        await Page.GotoAsync($"{_baseUrl}/Cart?restoreCart={snapshot}");

        await Expect(Page.Locator(".cart-item h2")).ToHaveTextAsync(product.Name);
        await Expect(Page.Locator(".quantity-stepper > span")).ToHaveTextAsync("2");
        Assert.That(await Page.GetByText("Betaling kommer snart").IsVisibleAsync(), Is.True);
    }

    private static async Task MigrateTestDatabaseAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(DatabaseConnectionString.Normalize(connectionString))
            .Options;
        await using var db = new OrderDbContext(options);
        await db.Database.MigrateAsync();
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Nordly.sln")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate Nordly.sln from the test output directory.");
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private async Task WaitForApplicationAsync(string applicationUrl, Process process)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));

        while (!timeout.IsCancellationRequested)
        {
            if (process.HasExited)
                throw new InvalidOperationException($"Nordly exited before becoming healthy.{Environment.NewLine}{GetServerOutput()}");

            try
            {
                using var response = await client.GetAsync($"{applicationUrl}/health", timeout.Token);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!timeout.IsCancellationRequested)
            {
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException($"Nordly did not become healthy within 45 seconds.{Environment.NewLine}{GetServerOutput()}");
    }

    private void AppendServerOutput(string? line)
    {
        if (line is null)
            return;

        lock (_serverOutputLock)
            _serverOutput.AppendLine(line);
    }

    private string GetServerOutput()
    {
        lock (_serverOutputLock)
            return _serverOutput.ToString();
    }
}
