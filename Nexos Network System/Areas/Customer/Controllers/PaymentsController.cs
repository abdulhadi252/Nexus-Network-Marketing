using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;

namespace Nexos_Network_System.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class PaymentsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record.";
            return View(new List<Payment>());
        }

        var payments = await db.Payments
            .Include(p => p.Bill)
            .Include(p => p.Connection)
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId.Value)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();

        return View(payments);
    }
}
