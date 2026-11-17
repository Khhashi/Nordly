using System.Net.Http.Json;

namespace Nordly.Web.Services;

public interface IPasswordlessLoginEmailSender
{
    Task<bool> SendAsync(string email, string code, CancellationToken cancellationToken = default);
}

public sealed class NoPasswordlessLoginEmailSender(
    ILogger<NoPasswordlessLoginEmailSender> logger) : IPasswordlessLoginEmailSender
{
    public Task<bool> SendAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Passwordless login email could not be sent because Brevo is not configured.");
        return Task.FromResult(false);
    }
}

public sealed class BrevoPasswordlessLoginEmailSender : IPasswordlessLoginEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoPasswordlessLoginEmailSender> _logger;

    public BrevoPasswordlessLoginEmailSender(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BrevoPasswordlessLoginEmailSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Email:BrevoApiKey"];
        var fromAddress = _configuration["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning("Passwordless login email could not be sent because Brevo is not configured.");
            return false;
        }

        var payload = new
        {
            sender = new { name = _configuration["Email:FromName"] ?? "Nordly", email = fromAddress },
            to = new[] { new { email } },
            subject = "Din engangskode for Nordly",
            textContent = $"Bruk koden {code} for å logge inn på Nordly. Koden utløper om 10 minutter."
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, BrevoOrderConfirmationEmailSender.Endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("api-key", apiKey);
        request.Headers.Accept.ParseAdd("application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return true;

            _logger.LogError("Brevo rejected passwordless login email: {StatusCode}", (int)response.StatusCode);
            return false;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Could not reach Brevo for passwordless login email.");
            return false;
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Brevo timed out while sending a passwordless login email.");
            return false;
        }
    }
}
