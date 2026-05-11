using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Utilities;

namespace BrownFlannelTavernStore.Pages.Admin.EmailLog;

[Authorize(Roles = "Owner,Manager")]
public class IndexModel : PageModel
{
    private const int EmailLogPageSize = 50;

    private readonly StoreDbContext _context;

    public IndexModel(StoreDbContext context)
    {
        _context = context;
    }

    public PagedList<Models.EmailLog> EmailLogs { get; set; } = new(Array.Empty<Models.EmailLog>(), 1, EmailLogPageSize, 0);
    public PaginationViewModel Pagination { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public EmailStatus? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public EmailType? TypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? RecipientSearchFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFromFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateToFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortDir { get; set; }

    public async Task OnGetAsync(int page = 1)
    {
        var query = _context.EmailLogs.AsQueryable();

        if (StatusFilter.HasValue)
            query = query.Where(e => e.Status == StatusFilter.Value);
        if (TypeFilter.HasValue)
            query = query.Where(e => e.EmailType == TypeFilter.Value);
        if (!string.IsNullOrWhiteSpace(RecipientSearchFilter))
        {
            var search = RecipientSearchFilter.Trim();
            query = query.Where(e => e.ToAddress.Contains(search));
        }
        if (DateFromFilter.HasValue)
            query = query.Where(e => e.CreatedAt >= DateFromFilter.Value);
        if (DateToFilter.HasValue)
            query = query.Where(e => e.CreatedAt < DateToFilter.Value.AddDays(1));

        query = (SortBy?.ToLowerInvariant(), SortDir?.ToLowerInvariant()) switch
        {
            ("recipient", "asc") => query.OrderBy(e => e.ToAddress),
            ("recipient", _) => query.OrderByDescending(e => e.ToAddress),
            ("type", "asc") => query.OrderBy(e => e.EmailType),
            ("type", _) => query.OrderByDescending(e => e.EmailType),
            ("subject", "asc") => query.OrderBy(e => e.Subject),
            ("subject", _) => query.OrderByDescending(e => e.Subject),
            ("status", "asc") => query.OrderBy(e => e.Status),
            ("status", _) => query.OrderByDescending(e => e.Status),
            ("updated", "asc") => query.OrderBy(e => e.DeliveryUpdatedAt),
            ("updated", _) => query.OrderByDescending(e => e.DeliveryUpdatedAt),
            ("date", "asc") => query.OrderBy(e => e.CreatedAt),
            _ => query.OrderByDescending(e => e.CreatedAt)
        };

        EmailLogs = await query.ToPagedListAsync(page, EmailLogPageSize);
        Pagination = PaginationViewModel.From(EmailLogs, "/Admin/EmailLog/Index", BuildRouteData());
    }

    public Dictionary<string, string?> BuildRouteData()
    {
        var data = new Dictionary<string, string?>();
        if (StatusFilter.HasValue) data[nameof(StatusFilter)] = StatusFilter.Value.ToString();
        if (TypeFilter.HasValue) data[nameof(TypeFilter)] = TypeFilter.Value.ToString();
        if (!string.IsNullOrWhiteSpace(RecipientSearchFilter)) data[nameof(RecipientSearchFilter)] = RecipientSearchFilter;
        if (DateFromFilter.HasValue) data[nameof(DateFromFilter)] = DateFromFilter.Value.ToString("yyyy-MM-dd");
        if (DateToFilter.HasValue) data[nameof(DateToFilter)] = DateToFilter.Value.ToString("yyyy-MM-dd");
        if (!string.IsNullOrWhiteSpace(SortBy)) data[nameof(SortBy)] = SortBy;
        if (!string.IsNullOrWhiteSpace(SortDir)) data[nameof(SortDir)] = SortDir;
        return data;
    }
}
