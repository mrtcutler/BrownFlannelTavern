namespace BrownFlannelTavernStore.Utilities;

public class SortableHeaderViewModel
{
    public required string PageName { get; init; }
    public required string ColumnKey { get; init; }
    public required string DisplayName { get; init; }
    public string? CurrentSort { get; init; }
    public string? CurrentDir { get; init; }
    public Dictionary<string, string?> RouteData { get; init; } = new();

    public bool IsActive => string.Equals(CurrentSort, ColumnKey, StringComparison.OrdinalIgnoreCase);

    public string NewDirection => IsActive && string.Equals(CurrentDir, "asc", StringComparison.OrdinalIgnoreCase)
        ? "desc"
        : "asc";

    public string Arrow => IsActive
        ? (string.Equals(CurrentDir, "desc", StringComparison.OrdinalIgnoreCase) ? " ▼" : " ▲")
        : "";

    public Dictionary<string, string?> SortRouteData()
    {
        var copy = new Dictionary<string, string?>(RouteData)
        {
            ["sortBy"] = ColumnKey,
            ["sortDir"] = NewDirection,
            ["page"] = null
        };
        return copy;
    }
}
