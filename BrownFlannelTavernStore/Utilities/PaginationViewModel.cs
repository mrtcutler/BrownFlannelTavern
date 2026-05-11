namespace BrownFlannelTavernStore.Utilities;

public class PaginationViewModel
{
    public required string PageName { get; init; }
    public required int CurrentPage { get; init; }
    public required int TotalPages { get; init; }
    public required int TotalCount { get; init; }
    public required int PageSize { get; init; }
    public Dictionary<string, string?> RouteData { get; init; } = new();

    public Dictionary<string, string?> RouteDataFor(int page)
    {
        var copy = new Dictionary<string, string?>(RouteData)
        {
            ["page"] = page.ToString()
        };
        return copy;
    }

    public static PaginationViewModel From<T>(
        PagedList<T> pagedList,
        string pageName,
        Dictionary<string, string?>? routeData = null) =>
        new()
        {
            PageName = pageName,
            CurrentPage = pagedList.Page,
            TotalPages = pagedList.TotalPages,
            TotalCount = pagedList.TotalCount,
            PageSize = pagedList.PageSize,
            RouteData = routeData ?? new Dictionary<string, string?>()
        };
}
