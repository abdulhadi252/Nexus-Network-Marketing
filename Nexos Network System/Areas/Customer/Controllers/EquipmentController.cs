using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;

namespace Nexos_Network_System.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class EquipmentController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record.";
            return View(new List<Equipment>());
        }

        var equipment = await db.Equipment
            .Include(e => e.Product)
            .Include(e => e.Connection)
            .AsNoTracking()
            .Where(e => e.Connection != null && e.Connection.CustomerId == customerId.Value)
            .OrderBy(e => e.AssetTag)
            .ToListAsync();

        return View(equipment);
    }
}
