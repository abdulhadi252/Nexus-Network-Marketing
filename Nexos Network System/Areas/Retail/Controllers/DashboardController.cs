using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Retail.Controllers;

[Area("Retail")]
[Authorize(Roles = "Retail")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Retail Panel";

        var retailShopId = await User.GetRetailShopIdAsync(db);
        if (retailShopId is null)
        {
            return Forbid();
        }

        var shopCityId = await db.RetailShops.Where(s => s.Id == retailShopId.Value)
            .Select(s => s.CityId)
            .FirstAsync();

        // The headline figures describe the current calendar month, matching the admin panel.
        var monthStart = MonthlySeries.StartOfMonth(DateTimeOffset.UtcNow);

        var model = new DashboardStatsViewModel
        {
            GreetingName = await User.GetGreetingNameAsync(db),
            PeriodLabel = $"{monthStart:d MMM yyyy} - {monthStart.AddMonths(1).AddDays(-1):d MMM yyyy}",

            TotalCustomers = await db.Customers.CountAsync(c => c.Addresses.Any(a => a.CityId == shopCityId)),
            TotalOrders = await db.Orders.CountAsync(o => o.RetailShopId == retailShopId.Value),
            PendingOrders = await db.Orders.CountAsync(o => o.RetailShopId == retailShopId.Value &&
                (o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending)),
            ActiveConnections = await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Active &&
                c.Order != null && c.Order.RetailShopId == retailShopId.Value),
            OutstandingBills = await db.Bills.LatestBillPerConnection().CountAsync(b => b.DueAmount > 0 &&
                b.Connection.Order != null && b.Connection.Order.RetailShopId == retailShopId.Value),
            PaymentsReceived = await db.Payments.Where(p => p.Status == PaymentStatus.Recorded &&
                p.Connection.Order != null && p.Connection.Order.RetailShopId == retailShopId.Value)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m,
            TotalPlans = await db.Plans.CountAsync(p => p.IsActive),
            RecentOrders = await db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Plan)
                .Include(o => o.Items)
                .Where(o => o.RetailShopId == retailShopId.Value)
                .OrderByDescending(o => o.SubmittedAt)
                .ThenByDescending(o => o.Id)
                .Take(8)
                .ToListAsync()
        };

        return View(model);
    }
}
