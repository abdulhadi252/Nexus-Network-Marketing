using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Areas.Retail.Controllers;

[Area("Retail")]
[Authorize(Roles = "Retail")]
public class BillsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, BillStatus? status, int page = 1)
    {
        ViewData["PanelName"] = "Retail Panel";
        ViewData["q"] = q;
        ViewData["status"] = status;

        var retailShopId = await User.GetRetailShopIdAsync(db);
        if (retailShopId is null)
        {
            return Forbid();
        }

        var query = db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection)
            .AsNoTracking()
            .Where(b => b.Connection.Order != null && b.Connection.Order.RetailShopId == retailShopId.Value)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(b =>
                b.BillNumber.Contains(term) ||
                b.Connection.AccountNumber.Contains(term) ||
                (b.Customer.FirstName + " " + b.Customer.LastName).Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        var result = await PagedResult<Bill>.CreateAsync(query.OrderByDescending(b => b.GeneratedAt), page);
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Retail Panel";

        var retailShopId = await User.GetRetailShopIdAsync(db);
        if (retailShopId is null)
        {
            return Forbid();
        }

        var bill = await db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection).ThenInclude(c => c.Plan)
            .Include(b => b.Items)
            .Include(b => b.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.Connection.Order != null &&
                                      b.Connection.Order.RetailShopId == retailShopId.Value);

        if (bill is null)
        {
            return NotFound();
        }

        return View(bill);
    }
}
