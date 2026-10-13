using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;

namespace Nexos_Network_System.Areas.Retail.Controllers;

[Area("Retail")]
[Authorize(Roles = "Retail")]
public class PaymentsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        ViewData["PanelName"] = "Retail Panel";
        ViewData["q"] = q;

        var retailShopId = await User.GetRetailShopIdAsync(db);
        if (retailShopId is null)
        {
            return Forbid();
        }

        var query = db.Payments
            .Include(p => p.Customer)
            .Include(p => p.Bill)
            .Include(p => p.Connection)
            .AsNoTracking()
            .Where(p => p.Connection.Order != null && p.Connection.Order.RetailShopId == retailShopId.Value)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                p.ReceiptNumber.Contains(term) ||
                (p.Customer.FirstName + " " + p.Customer.LastName).Contains(term) ||
                p.Connection.AccountNumber.Contains(term));
        }

        var result = await PagedResult<Payment>.CreateAsync(query.OrderByDescending(p => p.PaidAt), page);
        return View(result);
    }
}
