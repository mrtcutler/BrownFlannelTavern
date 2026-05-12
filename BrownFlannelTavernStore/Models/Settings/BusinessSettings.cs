namespace BrownFlannelTavernStore.Models.Settings;

public class BusinessSettings
{
    public const string SectionName = "BusinessSettings";

    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string StoreNameSuffix { get; set; } = "Store";
    public string Tagline { get; set; } = string.Empty;
    public int CopyrightYearStart { get; set; } = DateTime.UtcNow.Year;
    public PickupSettings Pickup { get; set; } = new();
    public ContactSettings Contact { get; set; } = new();

    public string FullStoreName => string.IsNullOrWhiteSpace(StoreNameSuffix)
        ? Name
        : $"{Name} {StoreNameSuffix}";
}

public class PickupSettings
{
    public bool Enabled { get; set; } = true;
    public string? LocationName { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Hours { get; set; }
    public string? Instructions { get; set; }

    public string FormattedAddress()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(AddressLine1)) parts.Add(AddressLine1);
        if (!string.IsNullOrWhiteSpace(AddressLine2)) parts.Add(AddressLine2);

        var cityStateZip = new[] { City, State, PostalCode }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        if (cityStateZip.Count > 0)
        {
            var cityState = string.Join(", ", cityStateZip.Take(2).Where(s => !string.IsNullOrWhiteSpace(s)));
            var line = string.IsNullOrWhiteSpace(PostalCode) ? cityState : $"{cityState} {PostalCode}".Trim();
            parts.Add(line);
        }

        return string.Join(", ", parts);
    }
}

public class ContactSettings
{
    public string? PublicEmail { get; set; }
    public string? PublicPhone { get; set; }
}
