using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Technical.Controllers;

[Area("Technical")]
[Authorize(Roles = "Technical")]
public class FeasibilityController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int page = 1)
    {
        ViewData["PanelName"] = "Technical Panel";

        var reports = await PagedResult<FeasibilityReport>.CreateAsync(
            db.FeasibilityReports
                .Include(f => f.Order).ThenInclude(o => o.Customer)
                .Include(f => f.TechnicalEmployee)
                .AsNoTracking()
                .OrderByDescending(f => f.Order.SubmittedAt),
            page);

        var pendingOrders = await db.Orders
            .Where(o => (o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending) && o.FeasibilityReport == null)
            .Include(o => o.Customer)
            .OrderBy(o => o.SubmittedAt)
            .ToListAsync();

        ViewBag.PendingOrders = pendingOrders;
        return View(reports);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Technical Panel";

        var report = await db.FeasibilityReports
            .Include(f => f.Order).ThenInclude(o => o.Customer).ThenInclude(c => c.Addresses).ThenInclude(a => a.City)
            .Include(f => f.Order).ThenInclude(o => o.Plan)
            .Include(f => f.TechnicalEmployee)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);

        if (report is null)
        {
            return NotFound();
        }

        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? orderId)
    {
        ViewData["PanelName"] = "Technical Panel";
        await LoadPendingOrdersAsync();

        var model = new FeasibilityFormViewModel();
        if (orderId.HasValue && await db.Orders.AnyAsync(o => o.Id == orderId.Value))
        {
            var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId.Value);
            model.OrderId = orderId.Value;
            model.OrderNumber = order?.OrderNumber ?? string.Empty;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeasibilityFormViewModel model)
    {
        ViewData["PanelName"] = "Technical Panel";

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == model.OrderId);
        if (order is null)
        {
            ModelState.AddModelError(nameof(model.OrderId), "The selected order is not available.");
        }
        else if (order.Status is not (OrderStatus.Pending or OrderStatus.FeasibilityPending) ||
                 await db.FeasibilityReports.AnyAsync(f => f.OrderId == model.OrderId))
        {
            ModelState.AddModelError(string.Empty, "A feasibility report can only be created for an order awaiting feasibility review.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPendingOrdersAsync();
            return View(model);
        }

        if (order is null)
        {
            await LoadPendingOrdersAsync();
            return View(model);
        }

        var employee = await User.GetEmployeeAsync(db);

        var report = new FeasibilityReport
        {
            OrderId = model.OrderId,
            TechnicalEmployeeId = employee?.Id,
            Status = model.Status,
            DistanceMeters = model.DistanceMeters,
            ServerAvailable = model.ServerAvailable,
            Remarks = model.Remarks,
            CheckedAt = DateTimeOffset.UtcNow
        };

        db.FeasibilityReports.Add(report);
        order.Status = model.Status switch
        {
            FeasibilityStatus.Feasible => OrderStatus.Feasible,
            FeasibilityStatus.NotFeasible => OrderStatus.NotFeasible,
            _ => OrderStatus.FeasibilityPending
        };
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        TempData["Success"] = $"Feasibility for order {order.OrderNumber} recorded as {model.Status}.";
        return RedirectToAction(nameof(Details), new { id = report.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["PanelName"] = "Technical Panel";

        var report = await db.FeasibilityReports
            .Include(f => f.Order).ThenInclude(o => o.Connection)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);

        if (report is null)
        {
            return NotFound();
        }

        if (report.Order.Connection is not null)
        {
            TempData["Error"] = "Feasibility cannot be changed after a connection has been created.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(new FeasibilityFormViewModel
        {
            Id = report.Id,
            OrderId = report.OrderId,
            OrderNumber = report.Order.OrderNumber,
            Status = report.Status,
            DistanceMeters = report.DistanceMeters,
            ServerAvailable = report.ServerAvailable,
            Remarks = report.Remarks
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FeasibilityFormViewModel model)
    {
        ViewData["PanelName"] = "Technical Panel";

        var report = await db.FeasibilityReports
            .Include(f => f.Order).ThenInclude(o => o.Connection)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (report is null)
        {
            return NotFound();
        }

        if (report.Order.Connection is not null)
        {
            TempData["Error"] = "Feasibility cannot be changed after a connection has been created.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!ModelState.IsValid)
        {
            model.OrderNumber = report.Order.OrderNumber;
            return View(model);
        }

        report.Status = model.Status;
        report.DistanceMeters = model.DistanceMeters;
        report.ServerAvailable = model.ServerAvailable;
        report.Remarks = model.Remarks;
        report.CheckedAt = DateTimeOffset.UtcNow;

        var employee = await User.GetEmployeeAsync(db);
        report.TechnicalEmployeeId = employee?.Id;

        report.Order.Status = model.Status switch
        {
            FeasibilityStatus.Feasible => OrderStatus.Feasible,
            FeasibilityStatus.NotFeasible => OrderStatus.NotFeasible,
            _ => OrderStatus.FeasibilityPending
        };
        report.Order.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        TempData["Success"] = "Feasibility report was updated.";
        return RedirectToAction(nameof(Details), new { id = report.Id });
    }

    private async Task LoadPendingOrdersAsync()
    {
        var orders = await db.Orders
            .Where(o => (o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending) && o.FeasibilityReport == null)
            .OrderBy(o => o.SubmittedAt)
            .Select(o => new { o.Id, Label = o.OrderNumber + " - " + o.Customer.FirstName + " " + o.Customer.LastName })
            .ToListAsync();

        ViewBag.OrderOptions = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(orders, "Id", "Label");
    }
}
