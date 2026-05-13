using Microsoft.Extensions.Options;

namespace BrownFlannelTavernStore.Models.Settings;

public class BusinessSettingsValidator : IValidateOptions<BusinessSettings>
{
    public ValidateOptionsResult Validate(string? name, BusinessSettings options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Name))
            errors.Add($"{BusinessSettings.SectionName}:{nameof(BusinessSettings.Name)} is required.");
        if (string.IsNullOrWhiteSpace(options.Tagline))
            errors.Add($"{BusinessSettings.SectionName}:{nameof(BusinessSettings.Tagline)} is required.");
        if (string.IsNullOrWhiteSpace(options.StoreNameSuffix))
            errors.Add($"{BusinessSettings.SectionName}:{nameof(BusinessSettings.StoreNameSuffix)} is required (use \"Store\" or similar — not empty).");

        if (options.Pickup.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.Pickup.LocationName))
                errors.Add($"{BusinessSettings.SectionName}:Pickup:LocationName is required when Pickup.Enabled = true.");
            if (string.IsNullOrWhiteSpace(options.Pickup.AddressLine1))
                errors.Add($"{BusinessSettings.SectionName}:Pickup:AddressLine1 is required when Pickup.Enabled = true.");
            if (string.IsNullOrWhiteSpace(options.Pickup.City))
                errors.Add($"{BusinessSettings.SectionName}:Pickup:City is required when Pickup.Enabled = true.");
            if (string.IsNullOrWhiteSpace(options.Pickup.State))
                errors.Add($"{BusinessSettings.SectionName}:Pickup:State is required when Pickup.Enabled = true.");
            if (string.IsNullOrWhiteSpace(options.Pickup.PostalCode))
                errors.Add($"{BusinessSettings.SectionName}:Pickup:PostalCode is required when Pickup.Enabled = true.");
            if (string.IsNullOrWhiteSpace(options.Pickup.Hours))
                errors.Add($"{BusinessSettings.SectionName}:Pickup:Hours is required when Pickup.Enabled = true.");
        }

        if (string.IsNullOrWhiteSpace(options.ShippingOrigin.AddressLine1))
            errors.Add($"{BusinessSettings.SectionName}:ShippingOrigin:AddressLine1 is required (source address for shipping rate calculation).");
        if (string.IsNullOrWhiteSpace(options.ShippingOrigin.City))
            errors.Add($"{BusinessSettings.SectionName}:ShippingOrigin:City is required.");
        if (string.IsNullOrWhiteSpace(options.ShippingOrigin.State))
            errors.Add($"{BusinessSettings.SectionName}:ShippingOrigin:State is required.");
        if (string.IsNullOrWhiteSpace(options.ShippingOrigin.PostalCode))
            errors.Add($"{BusinessSettings.SectionName}:ShippingOrigin:PostalCode is required.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
