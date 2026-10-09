using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;

namespace Nexos_Network_System.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class ConnectionController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record.";
            return View(new List<Connection>());
        }

        var connections = await db.Connections
            .Include(c => c.Plan)
            .Include(c => c.City)
            .Include(c => c.EquipmentItems).ThenInclude(e => e.Product)
            .Include(c => c.PlanHistory).ThenInclude(h => h.Plan)
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId.Value)
            .OrderBy(c => c.AccountNumber)
            .ToListAsync();

        return View(connections);
    }
}
