using System.Net;
using System.Text.Json;
using BrownFlannelTavernStore.Services.Notifications;
using BrownFlannelTavernStore.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace BrownFlannelTavernStore.Tests.Services.Notifications;

public class ResendEmailSenderTests
{
    private static IConfiguration BuildConfig(
        string? apiKey = "re_test_key",
        string? fromAddress = "noreply@example.com",
        string? fromName = "Brown Flannel Tavern")
    {
        var dict = new Dictionary<string, string?>();
        if (apiKey != null) dict["Resend:ApiKey"] = apiKey;
        if (fromAddress != null) dict["Resend:FromAddress"] = fromAddress;
        if (fromName != null) dict["Resend:FromName"] = fromName;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static ResendEmailSender BuildSender(TestHttpMessageHandler handler, IConfiguration config)
    {
        var httpClient = new HttpClient(handler);
        var logger = Mock.Of<ILogger<ResendEmailSender>>();
        return new ResendEmailSender(httpClient, config, logger);
    }

    private static EmailMessage SampleMessage() =>
        new(To: "recipient@example.com", Subject: "Test", HtmlBody: "<p>Hello</p>", TextBody: "Hello");

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
    public async Task SendAsync_SuccessfulResponse_Completes()
    {
        var config = BuildConfig();
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":\"abc123\"}")
        });
        var sender = BuildSender(handler, config);

        var act = () => sender.SendAsync(SampleMessage());

        await act.Should().NotThrowAsync();
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
    public async Task SendAsync_BuildsCorrectRequest()
    {
        var config = BuildConfig(apiKey: "re_secret_key", fromAddress: "noreply@example.com", fromName: "BFT Test");
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sender = BuildSender(handler, config);

        await sender.SendAsync(new EmailMessage(
            To: "customer@example.com",
            Subject: "Order #42",
            HtmlBody: "<h1>Thanks</h1>",
            TextBody: "Thanks"));

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().Be("https://api.resend.com/emails");
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("re_secret_key");

        var body = JsonDocument.Parse(handler.LastRequestBody!).RootElement;
        body.GetProperty("from").GetString().Should().Be("BFT Test <noreply@example.com>");
        body.GetProperty("to")[0].GetString().Should().Be("customer@example.com");
        body.GetProperty("subject").GetString().Should().Be("Order #42");
        body.GetProperty("html").GetString().Should().Be("<h1>Thanks</h1>");
        body.GetProperty("text").GetString().Should().Be("Thanks");
    }

    [Fact]
    public async Task SendAsync_FromNameMissing_UsesBareAddress()
    {
        var config = BuildConfig(fromName: null);
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sender = BuildSender(handler, config);

        await sender.SendAsync(SampleMessage());

        var body = JsonDocument.Parse(handler.LastRequestBody!).RootElement;
        body.GetProperty("from").GetString().Should().Be("noreply@example.com");
    }
}
