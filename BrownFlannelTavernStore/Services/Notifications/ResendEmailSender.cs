using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BrownFlannelTavernStore.Services.Notifications;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(HttpClient httpClient, IConfiguration configuration, ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Resend:ApiKey"]
            ?? throw new InvalidOperationException("Resend:ApiKey is not configured.");
        var fromAddress = _configuration["Resend:FromAddress"]
            ?? throw new InvalidOperationException("Resend:FromAddress is not configured.");
        var fromName = _configuration["Resend:FromName"];

        var fromHeader = string.IsNullOrWhiteSpace(fromName)
            ? fromAddress
            : $"{fromName} <{fromAddress}>";

        var payload = new
        {
            from = fromHeader,
            to = new[] { message.To },
            subject = message.Subject,
            html = message.HtmlBody,
            text = message.TextBody
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Resend API rejected send: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException(
                $"Failed to send email via Resend ({(int)response.StatusCode}): {body}");
        }

        _logger.LogInformation("Sent email via Resend to {To} (subject: {Subject})", message.To, message.Subject);
    }
}
