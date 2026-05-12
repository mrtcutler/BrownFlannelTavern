using BrownFlannelTavernStore.Models.Settings;

namespace BrownFlannelTavernStore.Tests.TestHelpers;

public static class TestBusiness
{
    public static BusinessSettings Default() => new()
    {
        Name = "Brown Flannel Tavern",
        ShortName = "BFT",
        StoreNameSuffix = "Store",
        Tagline = "Test tagline",
        CopyrightYearStart = 2026,
        Pickup = new PickupSettings
        {
            Enabled = true,
            LocationName = "Brown Flannel Tavern",
            AddressLine1 = "175 S Venoy Rd",
            City = "Westland",
            State = "MI",
            PostalCode = "48186",
            Hours = "11 AM – 2 AM, 7 days a week"
        }
    };
}
