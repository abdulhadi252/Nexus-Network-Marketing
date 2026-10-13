using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Technical.Controllers;

[Area("Technical")]
[Authorize(Roles = "Technical")]
public class ConnectionsController(ApplicationDbContext db, IConnectionService connectionService) : Controller
{
    public async Task<IActionResult> Index(string? q, ConnectionStatus? status, int page = 1)
    {
        ViewData["PanelName"] = "Technical Panel";
        ViewData["q"] = q;
        ViewData["status"] = status;

        var query = db.Connections
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .Include(c => c.City)
            .Include(c => c.EquipmentItems).ThenInclude(e => e.Product)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(c => c.AccountNumber.Contains(term) || (c.Customer.FirstName + " " + c.Customer.LastName).Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        var result = await PagedResult<Connection>.CreateAsync(query.OrderBy(c => c.AccountNumber), page);
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Technical Panel";

        var connection = await db.Connections
            .Include(c => c.Customer).ThenInclude(cu => cu.Addresses).ThenInclude(a => a.City)
            .Include(c => c.City)
            .Include(c => c.Plan)
            .Include(c => c.Order)
            .Include(c => c.EquipmentItems).ThenInclude(e => e.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (connection is null)
        {
            return NotFound();
        }

        return View(connection);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? orderId)
    {
        ViewData["PanelName"] = "Technical Panel";
        await LoadFeasibleOrdersAsync();
        return View(new ConnectionCreateViewModel { OrderId = orderId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ConnectionCreateViewModel model)
    {
        ViewData["PanelName"] = "Technical Panel";

        if (!ModelState.IsValid)
        {
            await LoadFeasibleOrdersAsync();
            return View(model);
        }

        try
        {
            var employee = await User.GetEmployeeAsync(db);
            var connectionId = await connectionService.CreateFromOrderAsync(model, employee?.Id);
            var connection = await db.Connections.AsNoTracking().FirstAsync(c => c.Id == connectionId);
            TempData["Success"] = $"Connection {connection.AccountNumber} was created for the order.";
            return RedirectToAction(nameof(Details), new { id = connectionId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadFeasibleOrdersAsync();
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, ConnectionStatus status, string? notes)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Choose a valid connection status.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await connectionService.UpdateStatusAsync(id, status, notes);
            TempData["Success"] = $"Connection status changed to {status}.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task LoadFeasibleOrdersAsync()
    {
        var orders = await db.Orders
            .Where(o => o.Status == OrderStatus.Feasible && o.Connection == null)
            .OrderBy(o => o.SubmittedAt)
            .Select(o => new
            {
                o.Id,
                Label = o.OrderNumber + " - " + o.Customer.FirstName + " " + o.Customer.LastName + " (" + o.ConnectionType + ")"
            })
            .ToListAsync();

        ViewBag.FeasibleOrders = new SelectList(orders, "Id", "Label");
    }
}
