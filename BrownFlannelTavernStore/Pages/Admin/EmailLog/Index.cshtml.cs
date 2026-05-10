using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Pages.Admin.EmailLog;

[Authorize(Roles = "Owner,Manager")]
public class IndexModel : PageModel
{
    private readonly StoreDbContext _context;

    public IndexModel(StoreDbContext context)
    {
        _context = context;
    }

    public List<Models.EmailLog> EmailLogs { get; set; } = [];

    public async Task OnGetAsync()
    {
        EmailLogs = await _context.EmailLogs
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .ToListAsync();
    }
}
