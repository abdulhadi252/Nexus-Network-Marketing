using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Technical.Controllers;

[Area("Technical")]
[Authorize(Roles = "Technical")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Technical Panel";

        var model = new DashboardStatsViewModel
        {
            PendingOrders = await db.Orders.CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending),
            PendingFeasibility = await db.FeasibilityReports.CountAsync(f => f.Status == FeasibilityStatus.Pending),
            ApprovedFeasibility = await db.FeasibilityReports.CountAsync(f => f.Status == FeasibilityStatus.Feasible),
            NotFeasibleCount = await db.FeasibilityReports.CountAsync(f => f.Status == FeasibilityStatus.NotFeasible),
            PendingConnections = await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Pending),
            ActiveConnections = await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Active),
            TotalEquipment = await db.Equipment.CountAsync(),
            AvailableEquipment = await db.Equipment.CountAsync(e => e.Status == EquipmentStatus.Available),
            PendingFeasibilityList = await db.FeasibilityReports
                .Include(f => f.Order).ThenInclude(o => o.Customer)
                .Where(f => f.Status == FeasibilityStatus.Pending)
                .OrderBy(f => f.Order.SubmittedAt)
                .Take(10)
                .ToListAsync(),
            PendingConnectionsList = await db.Connections
                .Where(c => c.Status == ConnectionStatus.Pending)
                .Include(c => c.Customer)
                .Include(c => c.Plan)
                .OrderBy(c => c.InstalledAt)
                .Take(10)
                .ToListAsync()
        };

        return View(model);
    }
}
