using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Areas.Technical.Controllers;

[Area("Technical")]
[Authorize(Roles = "Technical")]
public class OrdersController(ApplicationDbContext db, IOrderService orderService) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        ViewData["PanelName"] = "Technical Panel";
        ViewData["q"] = q;

        var query = db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.FeasibilityReport)
            .Include(o => o.Connection)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(o => o.OrderNumber.Contains(term) || (o.Customer.FirstName + " " + o.Customer.LastName).Contains(term));
        }

        var result = await PagedResult<Order>.CreateAsync(query.OrderBy(o => o.SubmittedAt), page);
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Technical Panel";

        var order = await db.Orders
            .Include(o => o.Customer).ThenInclude(c => c.Addresses).ThenInclude(a => a.City)
            .Include(o => o.Plan)
            .Include(o => o.RetailShop)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.FeasibilityReport).ThenInclude(f => f!.TechnicalEmployee)
            .Include(o => o.Connection)
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

        if (status is OrderStatus.Cancelled or OrderStatus.ConnectionProvided)
        {
            TempData["Error"] = "Technical staff cannot cancel orders or mark connections as provided directly.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (remarks?.Length > 2000)
        {
            TempData["Error"] = "Technical remarks cannot exceed 2,000 characters.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        try
        {
            await orderService.UpdateStatusAsync(id, status);
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
