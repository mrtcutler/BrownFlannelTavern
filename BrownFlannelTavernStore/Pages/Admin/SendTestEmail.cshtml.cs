using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Services.Notifications;

namespace BrownFlannelTavernStore.Pages.Admin;

[Authorize(Roles = SeedData.OwnerOrManagerRoles)]
public class SendTestEmailModel : PageModel
{
    private readonly IEmailSender _emailSender;

    public SendTestEmailModel(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    [BindProperty]
    public string To { get; set; } = string.Empty;

    [BindProperty]
    public string Subject { get; set; } = "Brown Flannel Tavern - Test Email";

    [BindProperty]
    public string Body { get; set; } = "Hello from the Brown Flannel Tavern store!";

    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        if (string.IsNullOrWhiteSpace(To))
        {
            To = User.Identity?.Name ?? string.Empty;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(To))
        {
            ErrorMessage = "Recipient address is required.";
            return Page();
        }

        try
        {
            await _emailSender.SendAsync(new EmailMessage(
                To: To,
                Subject: Subject,
                HtmlBody: $"<p>{Body}</p>",
                EmailType: EmailType.TestEmail,
                TextBody: Body));
            Message = $"Test email sent to {To}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return Page();
    }
}
