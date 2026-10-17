using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;

namespace Nexos_Network_System.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class BillsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record.";
            return View(new List<Bill>());
        }

        var bills = await db.Bills
            .Include(b => b.Connection)
            .Include(b => b.Payments)
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId.Value)
            .OrderByDescending(b => b.GeneratedAt)
            .ToListAsync();

        return View(bills);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            return NotFound();
        }

        var bill = await db.Bills
            .Include(b => b.Connection).ThenInclude(c => c.Plan)
            .Include(b => b.Items)
            .Include(b => b.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.CustomerId == customerId.Value);

        if (bill is null)
        {
            return NotFound();
        }

        return View(bill);
    }
}
