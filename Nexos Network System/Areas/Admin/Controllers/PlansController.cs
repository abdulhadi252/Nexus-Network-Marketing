using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PlansController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";
        ViewData["q"] = q;

        var query = db.Plans.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Name.Contains(term) || (p.Description != null && p.Description.Contains(term)));
        }

        var result = await PagedResult<Plan>.CreateAsync(query.OrderBy(p => p.ConnectionType).ThenBy(p => p.RecurringCharge), page);

        // Sparkline KPI tiles above the directory, Messages-page style.
        var allPlans = await db.Plans.AsNoTracking()
            .Include(p => p.Connections)
            .Include(p => p.Orders)
            .ToListAsync();

        ViewData["KpiTotal"] = AdminKpi.Static(allPlans.Count, "service plans");
        ViewData["KpiActive"] = AdminKpi.Static(allPlans.Count(p => p.IsActive), "active plans");
        ViewData["KpiConnections"] = AdminKpi.Static(allPlans.Sum(p => p.Connections.Count), "plan connections");
        ViewData["KpiOrders"] = AdminKpi.Static(allPlans.Sum(p => p.Orders.Count), "orders on plans");

        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var plan = await db.Plans
            .Include(p => p.Connections)
            .Include(p => p.Orders)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan is null)
        {
            return NotFound();
        }

        return View(plan);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["PanelName"] = "Admin Panel";
        return View(new PlanFormViewModel { ValidityDays = 30 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlanFormViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        if (await db.Plans.AnyAsync(p => p.Name == model.Name && p.ConnectionType == model.ConnectionType))
        {
            ModelState.AddModelError(nameof(model.Name), "A plan with this name already exists for this connection type.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        db.Plans.Add(new Plan
        {
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            ConnectionType = model.ConnectionType,
            BillingCycle = model.BillingCycle,
            ValidityDays = model.ValidityDays,
            SpeedMbps = model.SpeedMbps,
            IncludedUsageHours = model.IncludedUsageHours,
            RecurringCharge = model.RecurringCharge,
            SecurityDeposit = model.SecurityDeposit,
            InstallationCharge = model.InstallationCharge,
            IsActive = model.IsActive
        });
        await db.SaveChangesAsync();

        TempData["Success"] = $"Plan '{model.Name}' was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var plan = await db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null)
        {
            return NotFound();
        }

        return View(new PlanFormViewModel
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            ConnectionType = plan.ConnectionType,
            BillingCycle = plan.BillingCycle,
            ValidityDays = plan.ValidityDays,
            SpeedMbps = plan.SpeedMbps,
            IncludedUsageHours = plan.IncludedUsageHours,
            RecurringCharge = plan.RecurringCharge,
            SecurityDeposit = plan.SecurityDeposit,
            InstallationCharge = plan.InstallationCharge,
            IsActive = plan.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PlanFormViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        var plan = await db.Plans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null)
        {
            return NotFound();
        }

        if (await db.Plans.AnyAsync(p => p.Name == model.Name && p.ConnectionType == model.ConnectionType && p.Id != id))
        {
            ModelState.AddModelError(nameof(model.Name), "A plan with this name already exists for this connection type.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        plan.Name = model.Name.Trim();
        plan.Description = model.Description?.Trim();
        plan.ConnectionType = model.ConnectionType;
        plan.BillingCycle = model.BillingCycle;
        plan.ValidityDays = model.ValidityDays;
        plan.SpeedMbps = model.SpeedMbps;
        plan.IncludedUsageHours = model.IncludedUsageHours;
        plan.RecurringCharge = model.RecurringCharge;
        plan.SecurityDeposit = model.SecurityDeposit;
        plan.InstallationCharge = model.InstallationCharge;
        plan.IsActive = model.IsActive;
        await db.SaveChangesAsync();

        TempData["Success"] = "Plan details were updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var plan = await db.Plans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null)
        {
            return NotFound();
        }

        plan.IsActive = !plan.IsActive;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Plan '{plan.Name}' is now {(plan.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var plan = await db.Plans
            .Include(p => p.Connections)
            .Include(p => p.Orders)
            .Include(p => p.ConnectionPlanHistories)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan is null)
        {
            return NotFound();
        }

        return View(plan);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var plan = await db.Plans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null)
        {
            return NotFound();
        }

        // Connections and plan history reference a plan with a restrict foreign key, so a plan
        // that is still in use is deactivated instead of deleted. Orders simply lose the link.
        var isInUse = await db.Connections.AnyAsync(c => c.PlanId == id)
            || await db.ConnectionPlanHistories.AnyAsync(h => h.PlanId == id);

        if (isInUse)
        {
            plan.IsActive = false;
            await db.SaveChangesAsync();
            TempData["Success"] = $"Plan '{plan.Name}' is used by existing connections, so it was deactivated instead of deleted.";
            return RedirectToAction(nameof(Index));
        }

        db.Plans.Remove(plan);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Plan '{plan.Name}' was deleted.";
        return RedirectToAction(nameof(Index));
    }
}
