using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class OrdersController(ApplicationDbContext db, IOrderService orderService) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record.";
            return View(new List<Order>());
        }

        var orders = await db.Orders
            .Include(o => o.Plan)
            .Include(o => o.Connection)
            .Include(o => o.FeasibilityReport)
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId.Value)
            .OrderByDescending(o => o.SubmittedAt)
            .ToListAsync();

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? planId = null)
    {
        ViewData["PanelName"] = "Customer Panel";

        if (await User.GetCustomerIdAsync(db) is null)
        {
            return Forbid();
        }

        // When the visitor arrived from a plan detail page, preselect that plan so the
        // order can be submitted without choosing it again.
        var model = new CustomerOrderViewModel();
        if (planId is > 0 && await db.Plans.AnyAsync(p => p.Id == planId && p.IsActive))
        {
            model.PlanId = planId.Value;
        }

        await LoadPlansAsync(model.PlanId);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerOrderViewModel model)
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            return Forbid();
        }

        var plan = await db.Plans
            .AsNoTracking()
            .Where(p => p.Id == model.PlanId && p.IsActive)
            .Select(p => new { p.Id, p.ConnectionType })
            .FirstOrDefaultAsync();

        if (plan is null)
        {
            ModelState.AddModelError(nameof(model.PlanId), "Choose an available plan.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPlansAsync(model.PlanId);
            return View(model);
        }

        try
        {
            var orderId = await orderService.CreateOrderAsync(
                new OrderCreateViewModel
                {
                    CustomerId = customerId.Value,
                    ConnectionType = plan!.ConnectionType,
                    PlanId = plan.Id,
                    Notes = model.Notes?.Trim()
                },
                retailShopId: null,
                processedByEmployeeId: null);

            return RedirectToAction(nameof(Details), new { id = orderId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadPlansAsync(model.PlanId);
            return View(model);
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            return NotFound();
        }

        var order = await db.Orders
            .Include(o => o.Plan)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.FeasibilityReport)
            .Include(o => o.Connection)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId.Value);

        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    private async Task LoadPlansAsync(int selectedPlanId = 0)
    {
        // Materialise the plans before building the labels: concatenating the
        // ConnectionType enum inside the LINQ query would send its numeric value to SQL
        // and the dropdown would read "1 - Broadband Home 10 Mbps" instead of the type name.
        var plans = await db.Plans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.ConnectionType)
            .ThenBy(p => p.RecurringCharge)
            .ToListAsync();

        ViewBag.Plans = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            plans.Select(p => new
            {
                p.Id,
                Label = $"{p.ConnectionType} - {p.Name} (PKR {p.RecurringCharge})"
            }).ToList(),
            "Id",
            "Label");

        // The same plans are shown under the form as cards, so a customer can pick a plan
        // there and order it without touching the dropdown.
        ViewBag.PlanCards = plans
            .Select(p => new PlanCardViewModel
            {
                Plan = p,
                Selectable = true,
                IsSelected = p.Id == selectedPlanId,
                DetailsUrl = Url.Action("PlanDetails", "Home", new { area = string.Empty, id = p.Id })
            })
            .ToList();
    }
}
