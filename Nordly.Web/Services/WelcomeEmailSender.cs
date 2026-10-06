using System.Net.Http.Json;
using System.Text;

namespace Nordly.Web.Services;

public interface IWelcomeEmailSender
{
    Task SendAsync(string email, CancellationToken cancellationToken = default);
}

// Brukes når Brevo ikke er satt opp, for eksempel lokalt. Påmeldingen lagres uansett.
public sealed class NoWelcomeEmailSender : IWelcomeEmailSender
{
    public Task SendAsync(string email, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class BrevoWelcomeEmailSender : IWelcomeEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoWelcomeEmailSender> _logger;

    public BrevoWelcomeEmailSender(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BrevoWelcomeEmailSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string email, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Email:BrevoApiKey"];
        var fromAddress = _configuration["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning("Welcome email skipped because Brevo is not configured.");
            return;
        }

        var payload = new
        {
            sender = new { name = _configuration["Email:FromName"] ?? "Nordly", email = fromAddress },
            to = new[] { new { email } },
            subject = "Velkommen til Nordly",
            textContent = BuildBody()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, BrevoOrderConfirmationEmailSender.Endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("api-key", apiKey);
        request.Headers.Accept.ParseAdd("application/json");

        // En feil her skal ikke stoppe påmeldingen, så vi logger og går videre.
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Brevo rejected welcome email: {StatusCode} {Error}", (int)response.StatusCode, error);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Could not reach Brevo for welcome email.");
        }
    }

    internal static string BuildBody()
    {
        var body = new StringBuilder();
        body.AppendLine("Hei,");
        body.AppendLine();
        body.AppendLine("Takk for at du meldte deg på nyhetsbrevet til Nordly.");
        body.AppendLine("Du får høre om nye produkter og tilbud før alle andre.");
        body.AppendLine();
        body.AppendLine("Vennlig hilsen");
        body.AppendLine("Nordly");
        return body.ToString();
    }
}
