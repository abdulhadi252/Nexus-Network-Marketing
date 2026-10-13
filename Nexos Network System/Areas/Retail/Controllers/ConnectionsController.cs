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
public class ConnectionsController(ApplicationDbContext db) : Controller
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

        var query = db.Connections
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .Include(c => c.City)
            .AsNoTracking()
            .Where(c => c.Order != null && c.Order.RetailShopId == retailShopId.Value)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(c =>
                c.AccountNumber.Contains(term) ||
                (c.Customer.FirstName + " " + c.Customer.LastName).Contains(term));
        }

        var result = await PagedResult<Connection>.CreateAsync(query.OrderBy(c => c.AccountNumber), page);
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

        var connection = await db.Connections
            .Include(c => c.Customer)
            .Include(c => c.City)
            .Include(c => c.Plan)
            .Include(c => c.Order)
            .Include(c => c.EquipmentItems).ThenInclude(e => e.Product)
            .Include(c => c.Bills)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.Order != null && c.Order.RetailShopId == retailShopId.Value);

        if (connection is null)
        {
            return NotFound();
        }

        return View(connection);
    }
}
