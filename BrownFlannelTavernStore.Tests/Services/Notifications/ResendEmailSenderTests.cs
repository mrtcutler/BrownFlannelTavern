using System.Net;
using System.Text.Json;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Models.Settings;
using BrownFlannelTavernStore.Services.Notifications;
using BrownFlannelTavernStore.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BrownFlannelTavernStore.Tests.Services.Notifications;

public class ResendEmailSenderTests
{
    private static IConfiguration BuildConfig(
        string? apiKey = "re_test_key",
        string? fromAddress = "noreply@example.com")
    {
        var dict = new Dictionary<string, string?>();
        if (apiKey != null) dict["Resend:ApiKey"] = apiKey;
        if (fromAddress != null) dict["Resend:FromAddress"] = fromAddress;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static ResendEmailSender BuildSender(TestHttpMessageHandler handler, IConfiguration config, BusinessSettings? business = null)
    {
        var httpClient = new HttpClient(handler);
        var logger = Mock.Of<ILogger<ResendEmailSender>>();
        var options = Options.Create(business ?? TestBusiness.Default());
        return new ResendEmailSender(httpClient, config, options, logger);
    }

    private static EmailMessage SampleMessage() =>
        new(To: "recipient@example.com", Subject: "Test", HtmlBody: "<p>Hello</p>", EmailType: EmailType.TestEmail, TextBody: "Hello");

    [Fact]
    public async Task SendAsync_ApiKeyMissing_Throws()
    {
        var config = BuildConfig(apiKey: null);
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sender = BuildSender(handler, config);

        var act = () => sender.SendAsync(SampleMessage());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Resend:ApiKey*");
    }

    [Fact]
    public async Task SendAsync_FromAddressMissing_Throws()
    {
        var config = BuildConfig(fromAddress: null);
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sender = BuildSender(handler, config);

        var act = () => sender.SendAsync(SampleMessage());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Resend:FromAddress*");
    }

    [Fact]
    public async Task SendAsync_SuccessfulResponse_ReturnsProviderMessageId()
    {
        var config = BuildConfig();
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":\"abc123\"}")
        });
        var sender = BuildSender(handler, config);

        var result = await sender.SendAsync(SampleMessage());

        result.ProviderMessageId.Should().Be("abc123");
    }

    [Fact]
    public async Task SendAsync_FailedResponse_ThrowsWithErrorBody()
    {
        var config = BuildConfig();
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"Invalid recipient\"}")
        });
        var sender = BuildSender(handler, config);

        var act = () => sender.SendAsync(SampleMessage());

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("400");
        ex.Which.Message.Should().Contain("Invalid recipient");
    }

    [Fact]
    public async Task SendAsync_FromHeaderUsesBusinessNameAsDisplay()
    {
        var business = TestBusiness.Default();
        business.Name = "Sample Store Co.";
        var config = BuildConfig(apiKey: "re_secret_key", fromAddress: "noreply@example.com");
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":\"x\"}")
        });
        var sender = BuildSender(handler, config, business);

        await sender.SendAsync(new EmailMessage(
            To: "customer@example.com",
            Subject: "Order #42",
            HtmlBody: "<h1>Thanks</h1>",
            EmailType: EmailType.OrderConfirmation,
            TextBody: "Thanks"));

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.Authorization!.Parameter.Should().Be("re_secret_key");

        var body = JsonDocument.Parse(handler.LastRequestBody!).RootElement;
        body.GetProperty("from").GetString().Should().Be("Sample Store Co. <noreply@example.com>");
        body.GetProperty("to")[0].GetString().Should().Be("customer@example.com");
        body.GetProperty("subject").GetString().Should().Be("Order #42");
    }

    [Fact]
    public async Task SendAsync_BusinessNameEmpty_UsesBareAddress()
    {
        var business = TestBusiness.Default();
        business.Name = "";
        var config = BuildConfig();
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":\"x\"}")
        });
        var sender = BuildSender(handler, config, business);

        await sender.SendAsync(SampleMessage());

        var body = JsonDocument.Parse(handler.LastRequestBody!).RootElement;
        body.GetProperty("from").GetString().Should().Be("noreply@example.com");
    }
}
