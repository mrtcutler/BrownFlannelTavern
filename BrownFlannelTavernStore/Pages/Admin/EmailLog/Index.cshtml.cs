using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrownFlannelTavernStore.Data;
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

    public async Task OnGetAsync(int page = 1)
    {
        EmailLogs = await _context.EmailLogs
            .OrderByDescending(e => e.CreatedAt)
            .ToPagedListAsync(page, EmailLogPageSize);

        Pagination = PaginationViewModel.From(EmailLogs, "/Admin/EmailLog/Index");
    }
}
