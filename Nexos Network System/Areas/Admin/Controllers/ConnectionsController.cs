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
public class ConnectionsController(ApplicationDbContext db, IConnectionService connectionService) : Controller
{
    public async Task<IActionResult> Index(string? q, ConnectionStatus? status, ConnectionType? connectionType, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";

        var baseQuery = db.Connections
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .Include(c => c.City)
            .AsNoTracking()
            .AsQueryable();

        // Summary tiles: counts from the unfiltered set so the tiles stay stable while paging.
        var totalConnections = await baseQuery.CountAsync();
        var activeConnections = await baseQuery.CountAsync(c => c.Status == ConnectionStatus.Active);
        var pendingConnections = await baseQuery.CountAsync(c => c.Status == ConnectionStatus.Pending);
        var suspendedConnections = await baseQuery.CountAsync(c =>
            c.Status == ConnectionStatus.TemporarilyInactive ||
            c.Status == ConnectionStatus.PermanentlyInactive ||
            c.Status == ConnectionStatus.Cancelled);

        var query = baseQuery;

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(c =>
                c.AccountNumber.Contains(term) ||
                (c.Customer.FirstName + " " + c.Customer.LastName).Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (connectionType.HasValue)
        {
            query = query.Where(c => c.ConnectionType == connectionType.Value);
        }

        var connections = await PagedResult<Connection>.CreateAsync(query.OrderBy(c => c.AccountNumber), page);

        var model = new ConnectionIndexViewModel
        {
            Connections = connections,
            TotalConnections = totalConnections,
            ActiveConnections = activeConnections,
            PendingConnections = pendingConnections,
            SuspendedConnections = suspendedConnections,
            Query = q,
            Status = status,
            ConnectionType = connectionType
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var connection = await db.Connections
            .Include(c => c.Customer)
            .Include(c => c.City)
            .Include(c => c.Plan)
            .Include(c => c.Order)
            .Include(c => c.EquipmentItems).ThenInclude(e => e.Product)
            .Include(c => c.Bills)
            .Include(c => c.PlanHistory).ThenInclude(h => h.Plan)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (connection is null)
        {
            return NotFound();
        }

        return View(connection);
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
}
