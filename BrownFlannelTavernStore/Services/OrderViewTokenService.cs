using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using BrownFlannelTavernStore.Models.Settings;

namespace BrownFlannelTavernStore.Services;

public class OrderViewTokenService
{
    private readonly OrderViewSettings _settings;
    private readonly TimeProvider _timeProvider;

    public OrderViewTokenService(IOptions<OrderViewSettings> options, TimeProvider? timeProvider = null)
    {
        _settings = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public string GenerateToken(int orderId)
    {
        var expiresAt = _timeProvider.GetUtcNow().AddDays(_settings.ExpiryDays).ToUnixTimeSeconds();
        var payload = $"{orderId}.{expiresAt}";
        var signature = ComputeSignature(payload);
        return Base64UrlEncode(Encoding.UTF8.GetBytes($"{payload}.{signature}"));
    }

    public int? Validate(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        try
        {
            var decoded = Encoding.UTF8.GetString(Base64UrlDecode(token));
            var parts = decoded.Split('.');
            if (parts.Length != 3) return null;

            if (!int.TryParse(parts[0], out var orderId)) return null;
            if (!long.TryParse(parts[1], out var expiresAtUnix)) return null;

            var expectedSignature = ComputeSignature($"{parts[0]}.{parts[1]}");
            if (!FixedTimeEquals(parts[2], expectedSignature)) return null;

            if (DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix) < _timeProvider.GetUtcNow())
                return null;

            return orderId;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public string GetViewUrl(int orderId)
    {
        var token = GenerateToken(orderId);
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/Orders/View?token={Uri.EscapeDataString(token)}";
    }

    private string ComputeSignature(string payload)
    {
        var key = Encoding.UTF8.GetBytes(_settings.Secret);
        var data = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(key);
        return Base64UrlEncode(hmac.ComputeHash(data));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
    }
}
