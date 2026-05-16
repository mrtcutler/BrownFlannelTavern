using Microsoft.Extensions.Options;

namespace BrownFlannelTavernStore.Models.Settings;

public class OrderViewSettingsValidator : IValidateOptions<OrderViewSettings>
{
    private const int MinSecretLength = 32;

    public ValidateOptionsResult Validate(string? name, OrderViewSettings options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Secret))
            errors.Add($"{OrderViewSettings.SectionName}:{nameof(OrderViewSettings.Secret)} is required (long random string used to HMAC-sign customer order-view links).");
        else if (options.Secret.Length < MinSecretLength)
            errors.Add($"{OrderViewSettings.SectionName}:{nameof(OrderViewSettings.Secret)} must be at least {MinSecretLength} characters to be cryptographically meaningful.");

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            errors.Add($"{OrderViewSettings.SectionName}:{nameof(OrderViewSettings.BaseUrl)} is required (public URL of the site, used to build magic links in emails — e.g., https://bft.tylercutler.com).");
        else if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
            errors.Add($"{OrderViewSettings.SectionName}:{nameof(OrderViewSettings.BaseUrl)} must be an absolute URL.");

        if (options.ExpiryDays <= 0)
            errors.Add($"{OrderViewSettings.SectionName}:{nameof(OrderViewSettings.ExpiryDays)} must be greater than 0.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
