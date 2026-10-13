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
public class ConnectionsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, ConnectionStatus? status, int page = 1)
    {
        ViewData["PanelName"] = "Accounts Panel";
        ViewData["q"] = q;
        ViewData["status"] = status;

        var query = db.Connections
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .Include(c => c.City)
            .Include(c => c.Bills)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(c => c.AccountNumber.Contains(term) || (c.Customer.FirstName + " " + c.Customer.LastName).Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        var result = await PagedResult<Connection>.CreateAsync(query.OrderBy(c => c.AccountNumber), page);
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Accounts Panel";

        var connection = await db.Connections
            .Include(c => c.Customer)
            .Include(c => c.City)
            .Include(c => c.Plan)
            .Include(c => c.Bills).ThenInclude(b => b.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (connection is null)
        {
            return NotFound();
        }

        return View(connection);
    }
}
