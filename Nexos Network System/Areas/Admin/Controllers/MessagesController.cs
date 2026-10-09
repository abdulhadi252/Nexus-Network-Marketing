using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MessagesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(bool unreadOnly = false, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";
        ViewData["unreadOnly"] = unreadOnly;

        var query = db.ContactMessages.AsNoTracking().AsQueryable();

        if (unreadOnly)
        {
            query = query.Where(m => !m.IsRead);
        }

        var result = await PagedResult<ContactMessage>.CreateAsync(query.OrderByDescending(m => m.SubmittedAt), page);
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRead(int id, bool unreadOnly = false, int page = 1)
    {
        var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (message is null)
        {
            return NotFound();
        }

        message.IsRead = !message.IsRead;
        await db.SaveChangesAsync();

        TempData["Success"] = message.IsRead ? "Message marked as read." : "Message marked as unread.";
        return RedirectToAction(nameof(Index), new { unreadOnly, page });
    }
}
