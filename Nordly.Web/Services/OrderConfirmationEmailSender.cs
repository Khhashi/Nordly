using System.Net;
using System.Net.Mail;
using System.Text;
using PG3302.Domain.Entities;

namespace Nordly.Web.Services;

public interface IOrderConfirmationEmailSender
{
    Task SendAsync(Order order, CancellationToken cancellationToken = default);
}

public sealed class SmtpOrderConfirmationEmailSender : IOrderConfirmationEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpOrderConfirmationEmailSender> _logger;

    public SmtpOrderConfirmationEmailSender(
        IConfiguration configuration,
        ILogger<SmtpOrderConfirmationEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(order.CustomerEmail))
            return;

        var host = _configuration["Email:SmtpHost"];
        var fromAddress = _configuration["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning("Order confirmation email skipped because SMTP is not configured.");
            return;
        }

        var port = _configuration.GetValue("Email:SmtpPort", 587);
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = _configuration.GetValue("Email:EnableSsl", true)
        };

        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, _configuration["Email:FromName"] ?? "Nordly"),
            Subject = $"Ordrebekreftelse for ordre {order.Id}",
            Body = BuildBody(order),
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(order.CustomerEmail));

        await client.SendMailAsync(message, cancellationToken);
    }

    private static string BuildBody(Order order)
    {
        var body = new StringBuilder();
        body.AppendLine($"Hei {order.CustomerName ?? "der"},");
        body.AppendLine();
        body.AppendLine("Takk for bestillingen din hos Nordly.");
        body.AppendLine($"Ordrenummer: {order.Id}");
        body.AppendLine();
        body.AppendLine("Produkter:");
        foreach (var line in order.OrderLines)
            body.AppendLine($"- {line.Product.Name} x {line.Quantity}: {(line.Product.Price * line.Quantity):N2} kr");

        body.AppendLine();
        body.AppendLine($"Produkter totalt: {order.GetTotal():N2} kr");
        body.AppendLine($"Levering: {(order.ShippingCost == 0 ? "Gratis" : $"{order.ShippingCost:N2} kr")}");
        body.AppendLine($"Totalt: {order.GetGrandTotal():N2} kr");
        body.AppendLine();
        body.AppendLine($"Leveringsadresse: {order.ShippingAddress ?? "Ikke oppgitt"}");
        body.AppendLine();
        body.AppendLine("Vennlig hilsen");
        body.AppendLine("Nordly");
        return body.ToString();
    }
}