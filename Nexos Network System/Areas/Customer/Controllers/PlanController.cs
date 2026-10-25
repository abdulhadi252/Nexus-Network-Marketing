using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;

namespace Nexos_Network_System.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class PlanController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record.";
            return View(new List<Plan>());
        }

        var plans = await db.Connections
            .Where(c => c.CustomerId == customerId.Value)
            .Select(c => c.Plan)
            .Distinct()
            .AsNoTracking()
            .ToListAsync();

        // Every other active plan is offered underneath, so the customer can read a plan's
        // details and order it from the panel instead of asking the branch.
        var currentPlanIds = plans.Select(p => p.Id).ToList();
        ViewBag.OtherPlans = await db.Plans
            .AsNoTracking()
            .Where(p => p.IsActive && !currentPlanIds.Contains(p.Id))
            .OrderBy(p => p.ConnectionType)
            .ThenBy(p => p.RecurringCharge)
            .ToListAsync();

        return View(plans);
    }
}
