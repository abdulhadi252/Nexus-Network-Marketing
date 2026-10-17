using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.Services;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController(ApplicationDbContext db, IOrderService orderService) : Controller
{
    public async Task<IActionResult> Index(OrderFilterViewModel filter, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";
        filter.Page = page;

        var query = db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.RetailShop)
            .Include(o => o.Connection)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var term = filter.Query.Trim();
            query = query.Where(o =>
                o.OrderNumber.Contains(term) ||
                (o.Customer.FirstName + " " + o.Customer.LastName).Contains(term));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        if (filter.ConnectionType.HasValue)
        {
            query = query.Where(o => o.ConnectionType == filter.ConnectionType.Value);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(o => o.SubmittedAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(o => o.SubmittedAt < filter.To.Value.AddDays(1));
        }

        var paged = await PagedResult<Order>.CreateAsync(query.OrderByDescending(o => o.SubmittedAt), page);

        // KPI tiles above the list use the same six-month monthly series the
        // dashboard sparklines draw from, so every admin list reads the same.
        var allOrders = db.Orders.AsNoTracking();
        var submitted = allOrders.Select(o => o.SubmittedAt);
        var pendingCount = await allOrders.CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending);
        var liveCount = await allOrders.CountAsync(o => o.Status == OrderStatus.ConnectionProvided || o.Status == OrderStatus.Feasible);

        ViewData["TotalOrders"] = await AdminKpi.CountAsync(submitted, db, "Total Orders");
        ViewData["PendingOrders"] = new BillMetric { Value = pendingCount, Foot = "Pending Orders", ShowFootArrow = false };
        ViewData["CompletedOrders"] = new BillMetric { Value = liveCount, Foot = "Live Services", ShowFootArrow = true, FootRises = true };
        ViewData["ThisMonthOrders"] = new BillMetric { Value = paged.TotalItems, Foot = "Matching Filters", ShowFootArrow = false };

        return View(paged);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var order = await db.Orders
            .Include(o => o.Customer).ThenInclude(c => c.Addresses).ThenInclude(a => a.City)
            .Include(o => o.RetailShop).ThenInclude(s => s!.City)
            .Include(o => o.Plan)
            .Include(o => o.ProcessedByEmployee)
            .Include(o => o.VerifiedByEmployee)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.FeasibilityReport).ThenInclude(f => f!.TechnicalEmployee)
            .Include(o => o.Connection)
            .Include(o => o.StatusHistory).ThenInclude(h => h.ChangedByEmployee)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, string? remarks)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Choose a valid order status.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (remarks?.Length > 2000)
        {
            TempData["Error"] = "Order remarks cannot exceed 2,000 characters.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        try
        {
            var employee = await User.GetEmployeeAsync(db);
            await orderService.UpdateStatusAsync(id, status, employee?.Id, remarks);
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                var tracked = await db.Orders.FirstAsync(o => o.Id == id);
                tracked.Remarks = remarks;
                await db.SaveChangesAsync();
            }

            TempData["Success"] = $"Order {order.OrderNumber} status changed to {status}.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
