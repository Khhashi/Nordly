using System.Net.Http.Json;
using Nordly.Domain.Entities;

namespace Nordly.Web.Services;

public sealed class BrevoOrderConfirmationEmailSender : IOrderConfirmationEmailSender
{
    public const string Endpoint = "https://api.brevo.com/v3/smtp/email";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoOrderConfirmationEmailSender> _logger;

    public BrevoOrderConfirmationEmailSender(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BrevoOrderConfirmationEmailSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(order.CustomerEmail))
            return;

        var apiKey = _configuration["Email:BrevoApiKey"];
        var fromAddress = _configuration["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning("Order confirmation email skipped because Brevo is not configured.");
            return;
        }

        var payload = new
        {
            sender = new { name = _configuration["Email:FromName"] ?? "Nordly", email = fromAddress },
            to = new[] { new { email = order.CustomerEmail } },
            subject = $"Ordrebekreftelse for ordre {order.Id}",
            textContent = SmtpOrderConfirmationEmailSender.BuildBody(order)
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("api-key", apiKey);
        request.Headers.Accept.ParseAdd("application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Brevo rejected order confirmation for order {OrderId}: {StatusCode} {Error}",
                    order.Id, (int)response.StatusCode, error);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not reach Brevo for order {OrderId}.", order.Id);
        }
    }
}
