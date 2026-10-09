using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize(Roles = "Accounts")]
public class DuesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, bool overdueOnly = false, int page = 1)
    {
        ViewData["PanelName"] = "Accounts Panel";
        ViewData["q"] = q;
        ViewData["overdueOnly"] = overdueOnly;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection)
            .AsNoTracking()
            .LatestBillPerConnection()
            .Where(b => b.DueAmount > 0 && b.Status != BillStatus.Cancelled);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(b =>
                b.Connection.AccountNumber.Contains(term) ||
                (b.Customer.FirstName + " " + b.Customer.LastName).Contains(term));
        }

        if (overdueOnly)
        {
            query = query.Where(b => b.DueDate < today);
        }

        var paged = await PagedResult<Bill>.CreateAsync(query.OrderBy(b => b.DueDate), page);

        ViewBag.TotalOutstanding = await db.Bills.LatestBillPerConnection()
            .Where(b => b.DueAmount > 0 && b.Status != BillStatus.Cancelled)
            .SumAsync(b => (decimal?)b.DueAmount) ?? 0m;

        ViewBag.OverdueCount = await db.Bills.LatestBillPerConnection()
            .CountAsync(b => b.DueAmount > 0 && b.Status != BillStatus.Cancelled && b.DueDate < today);

        ViewBag.OverdueAmount = await db.Bills.LatestBillPerConnection()
            .Where(b => b.DueAmount > 0 && b.Status != BillStatus.Cancelled && b.DueDate < today)
            .SumAsync(b => (decimal?)b.DueAmount) ?? 0m;

        return View(paged);
    }
}
