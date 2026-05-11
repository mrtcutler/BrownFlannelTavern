using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Utilities;

namespace BrownFlannelTavernStore.Pages.Admin.Orders;

public static class OrdersSortKeys
{
    public const string Id = "id";
    public const string Customer = "customer";
    public const string Total = "total";
    public const string Status = "status";
    public const string Date = "date";
}

[Authorize(Roles = SeedData.OwnerOrManagerRoles)]
public class IndexModel : PageModel
{
    private readonly StoreDbContext _context;

    public IndexModel(StoreDbContext context)
    {
        _context = context;
    }

    public PagedList<Order> Orders { get; set; } = new(Array.Empty<Order>(), 1, PagedListExtensions.DefaultPageSize, 0);
    public PaginationViewModel Pagination { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public OrderStatus? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? OrderIdFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFromFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateToFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CustomerSearchFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortDir { get; set; }

    public async Task OnGetAsync(int page = 1)
    {
        var query = _context.Orders.AsQueryable();

        if (StatusFilter.HasValue)
            query = query.Where(o => o.Status == StatusFilter.Value);
        if (OrderIdFilter.HasValue)
            query = query.Where(o => o.Id == OrderIdFilter.Value);
        if (DateFromFilter.HasValue)
            query = query.Where(o => o.CreatedAt >= DateFromFilter.Value);
        if (DateToFilter.HasValue)
            query = query.Where(o => o.CreatedAt < DateToFilter.Value.AddDays(1));
        if (!string.IsNullOrWhiteSpace(CustomerSearchFilter))
        {
            var search = CustomerSearchFilter.Trim();
            query = query.Where(o => o.CustomerName.Contains(search) || o.CustomerEmail.Contains(search));
        }

        query = (SortBy?.ToLowerInvariant(), SortDir?.ToLowerInvariant()) switch
        {
            (OrdersSortKeys.Id, SortDirection.Ascending) => query.OrderBy(o => o.Id),
            (OrdersSortKeys.Id, _) => query.OrderByDescending(o => o.Id),
            (OrdersSortKeys.Customer, SortDirection.Ascending) => query.OrderBy(o => o.CustomerName),
            (OrdersSortKeys.Customer, _) => query.OrderByDescending(o => o.CustomerName),
            (OrdersSortKeys.Total, SortDirection.Ascending) => query.OrderBy(o => o.TotalAmount),
            (OrdersSortKeys.Total, _) => query.OrderByDescending(o => o.TotalAmount),
            (OrdersSortKeys.Status, SortDirection.Ascending) => query.OrderBy(o => o.Status),
            (OrdersSortKeys.Status, _) => query.OrderByDescending(o => o.Status),
            (OrdersSortKeys.Date, SortDirection.Ascending) => query.OrderBy(o => o.CreatedAt),
            _ => query.OrderByDescending(o => o.CreatedAt)
        };

        Orders = await query.ToPagedListAsync(page);

        Pagination = PaginationViewModel.From(Orders, "/Admin/Orders/Index", BuildRouteData());
    }

    public Dictionary<string, string?> BuildRouteData()
    {
        var data = new Dictionary<string, string?>();
        if (StatusFilter.HasValue) data[nameof(StatusFilter)] = StatusFilter.Value.ToString();
        if (OrderIdFilter.HasValue) data[nameof(OrderIdFilter)] = OrderIdFilter.Value.ToString();
        if (DateFromFilter.HasValue) data[nameof(DateFromFilter)] = DateFromFilter.Value.ToString("yyyy-MM-dd");
        if (DateToFilter.HasValue) data[nameof(DateToFilter)] = DateToFilter.Value.ToString("yyyy-MM-dd");
        if (!string.IsNullOrWhiteSpace(CustomerSearchFilter)) data[nameof(CustomerSearchFilter)] = CustomerSearchFilter;
        if (!string.IsNullOrWhiteSpace(SortBy)) data[nameof(SortBy)] = SortBy;
        if (!string.IsNullOrWhiteSpace(SortDir)) data[nameof(SortDir)] = SortDir;
        return data;
    }
}
