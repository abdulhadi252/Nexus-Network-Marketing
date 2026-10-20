using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;

namespace Nexos_Network_System.Areas.Customer.Controllers;

/// <summary>
/// The customer's own inbox. Accounts writes into it when it sends a bill notification, and the
/// rows are scoped to the signed-in customer only.
/// </summary>
[Area("Customer")]
[Authorize(Roles = "Customer")]
public class NotificationsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record.";
            return View(new List<CustomerNotification>());
        }

        var notifications = await db.CustomerNotifications
            .Include(n => n.Bill)
            .AsNoTracking()
            .Where(n => n.CustomerId == customerId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .ToListAsync();

        var unreadIds = notifications.Where(n => !n.IsRead).Select(n => n.Id).ToList();
        ViewData["UnreadCount"] = unreadIds.Count;

        // Pay now is only offered for the bill that can actually be paid right now: the latest bill
        // of its connection, still carrying a due. An older bill is shown read-only.
        ViewData["PayableBillIds"] = (await db.Bills
                .LatestBillPerConnection()
                .Where(b => b.CustomerId == customerId.Value && b.DueAmount > 0)
                .Select(b => b.Id)
                .ToListAsync())
            .ToHashSet();

        // Opening the inbox acknowledges the messages, so the bell stops counting them.
        if (unreadIds.Count > 0)
        {
            await db.CustomerNotifications
                .Where(n => unreadIds.Contains(n.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow));
        }

        return View(notifications);
    }
}
