using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record. Please contact support.";
            return View(new CustomerDashboardViewModel());
        }

        // The headline figures describe the current calendar month, matching the other panels.
        var monthStart = MonthlySeries.StartOfMonth(DateTimeOffset.UtcNow);

        var connections = await db.Connections
            .Include(c => c.Plan)
            .Include(c => c.City)
            .Include(c => c.EquipmentItems).ThenInclude(e => e.Product)
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId.Value)
            .OrderBy(c => c.AccountNumber)
            .ToListAsync();

        var currentBill = await db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection)
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId.Value)
            .OrderByDescending(b => b.GeneratedAt)
            .ThenByDescending(b => b.Id)
            .FirstOrDefaultAsync();

        var model = new CustomerDashboardViewModel
        {
            GreetingName = await User.GetGreetingNameAsync(db),
            PeriodLabel = $"{monthStart:d MMM yyyy} - {monthStart.AddMonths(1).AddDays(-1):d MMM yyyy}",

            Connections = connections,
            CurrentBill = currentBill,
            OutstandingAmount = await db.Bills.LatestBillPerConnection()
                .Where(b => b.CustomerId == customerId.Value && b.DueAmount > 0)
                .SumAsync(b => (decimal?)b.DueAmount) ?? 0m,
            PendingOrders = await db.Orders.CountAsync(o => o.CustomerId == customerId.Value &&
                (o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending)),
            PaymentsMade = await db.Payments
                .Where(p => p.CustomerId == customerId.Value && p.Status == PaymentStatus.Recorded)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m,

            // The shared order row renders the customer and plan, so both are loaded with the order.
            RecentOrders = await db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Plan)
                .Include(o => o.Items)
                .AsNoTracking()
                .Where(o => o.CustomerId == customerId.Value)
                .OrderByDescending(o => o.SubmittedAt)
                .ThenByDescending(o => o.Id)
                .Take(5)
                .ToListAsync(),
            RecentPayments = await db.Payments
                .Include(p => p.Bill)
                .Include(p => p.Connection)
                .AsNoTracking()
                .Where(p => p.CustomerId == customerId.Value)
                .OrderByDescending(p => p.PaidAt)
                .ThenByDescending(p => p.Id)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }
}
