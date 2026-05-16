using BrownFlannelTavernStore.Models.Settings;
using BrownFlannelTavernStore.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace BrownFlannelTavernStore.Tests.Services;

public class OrderViewTokenServiceTests
{
    private const string TestSecret = "test-secret-that-is-at-least-32-characters-long-for-hmac";

    private static OrderViewTokenService BuildService(int expiryDays = 90, TimeProvider? timeProvider = null)
    {
        var settings = new OrderViewSettings
        {
            Secret = TestSecret,
            BaseUrl = "https://example.com",
            ExpiryDays = expiryDays,
        };
        return new OrderViewTokenService(Options.Create(settings), timeProvider);
    }

    [Fact]
    public void GenerateToken_ThenValidate_ReturnsSameOrderId()
    {
        var service = BuildService();
        var token = service.GenerateToken(42);

        var validated = service.Validate(token);

        validated.Should().Be(42);
    }

    [Fact]
    public void Validate_TamperedToken_ReturnsNull()
    {
        var service = BuildService();
        var token = service.GenerateToken(42);
        var tampered = token[..^3] + "AAA";

        var validated = service.Validate(tampered);

        validated.Should().BeNull();
    }

    [Fact]
    public void Validate_TokenSignedWithDifferentSecret_ReturnsNull()
    {
        var service1 = BuildService();
        var settings2 = new OrderViewSettings
        {
            Secret = "completely-different-secret-but-also-32-characters-or-more",
            BaseUrl = "https://example.com",
            ExpiryDays = 90,
        };
        var service2 = new OrderViewTokenService(Options.Create(settings2));

        var token = service1.GenerateToken(42);
        var validated = service2.Validate(token);

        validated.Should().BeNull();
    }

    [Fact]
    public void Validate_ExpiredToken_ReturnsNull()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var service = BuildService(expiryDays: 1, timeProvider: fakeTime);
        var token = service.GenerateToken(42);

        fakeTime.Advance(TimeSpan.FromDays(2));

        var validated = service.Validate(token);
        validated.Should().BeNull();
    }

    [Fact]
    public void Validate_NotYetExpiredToken_ReturnsOrderId()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var service = BuildService(expiryDays: 1, timeProvider: fakeTime);
        var token = service.GenerateToken(42);

        fakeTime.Advance(TimeSpan.FromHours(23));

        var validated = service.Validate(token);
        validated.Should().Be(42);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-token")]
    [InlineData("garbage.with.dots")]
    public void Validate_MalformedOrEmptyToken_ReturnsNull(string? token)
    {
        var service = BuildService();

        var validated = service.Validate(token);

        validated.Should().BeNull();
    }

    [Fact]
    public void GetViewUrl_ProducesUrlWithBaseUrlAndUrlEncodedToken()
    {
        var service = BuildService();
        var url = service.GetViewUrl(42);

        url.Should().StartWith("https://example.com/Orders/View?token=");
        url.Should().NotContain(" ");
        var token = url.Split("token=")[1];
        var decodedToken = Uri.UnescapeDataString(token);
        service.Validate(decodedToken).Should().Be(42);
    }

    [Fact]
    public void GetViewUrl_StripsTrailingSlashFromBaseUrl()
    {
        var settings = new OrderViewSettings
        {
            Secret = TestSecret,
            BaseUrl = "https://example.com/",
            ExpiryDays = 90,
        };
        var service = new OrderViewTokenService(Options.Create(settings));

        var url = service.GetViewUrl(42);

        url.Should().StartWith("https://example.com/Orders/View?token=");
        url.Should().NotStartWith("https://example.com//");
    }

    private class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;
        public FakeTimeProvider(DateTimeOffset start) => _now = start;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
    }
}
