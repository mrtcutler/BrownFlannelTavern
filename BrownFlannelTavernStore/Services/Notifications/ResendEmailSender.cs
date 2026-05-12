using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BrownFlannelTavernStore.Models.Settings;
using Microsoft.Extensions.Options;

namespace BrownFlannelTavernStore.Services.Notifications;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IOptions<BusinessSettings> _businessOptions;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(HttpClient httpClient, IConfiguration configuration, IOptions<BusinessSettings> businessOptions, ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _businessOptions = businessOptions;
        _logger = logger;
    }

    public virtual async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Resend:ApiKey"]
            ?? throw new InvalidOperationException("Resend:ApiKey is not configured.");
        var fromAddress = _configuration["Resend:FromAddress"]
            ?? throw new InvalidOperationException("Resend:FromAddress is not configured.");
        var businessName = _businessOptions.Value.Name;

        var fromHeader = string.IsNullOrWhiteSpace(businessName)
            ? fromAddress
            : $"{businessName} <{fromAddress}>";

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
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Resend API rejected send: {Status} {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException(
                $"Failed to send email via Resend ({(int)response.StatusCode}): {responseBody}");
        }

        var providerMessageId = JsonDocument.Parse(responseBody)
            .RootElement
            .GetProperty("id")
            .GetString() ?? string.Empty;

        _logger.LogInformation("Sent email via Resend to {To} (subject: {Subject}, id: {Id})",
            message.To, message.Subject, providerMessageId);

        return new EmailSendResult(providerMessageId);
    }
}
