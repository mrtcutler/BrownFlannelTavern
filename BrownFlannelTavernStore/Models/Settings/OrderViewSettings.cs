namespace BrownFlannelTavernStore.Models.Settings;

public class OrderViewSettings
{
    public const string SectionName = "OrderViewSettings";

    public string Secret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public int ExpiryDays { get; set; } = 90;
}
