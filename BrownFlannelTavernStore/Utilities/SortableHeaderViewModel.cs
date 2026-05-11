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

    public string NewDirection => IsActive && string.Equals(CurrentDir, SortDirection.Ascending, StringComparison.OrdinalIgnoreCase)
        ? SortDirection.Descending
        : SortDirection.Ascending;

    public string Arrow => IsActive
        ? (string.Equals(CurrentDir, SortDirection.Descending, StringComparison.OrdinalIgnoreCase) ? " ▼" : " ▲")
        : "";

    public Dictionary<string, string?> SortRouteData()
    {
        var copy = new Dictionary<string, string?>(RouteData)
        {
            ["SortBy"] = ColumnKey,
            ["SortDir"] = NewDirection,
            ["page"] = null
        };
        return copy;
    }
}
