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

        var connections = await db.Connections
            .Include(c => c.Plan)
            .Include(c => c.City)
            .Include(c => c.EquipmentItems).ThenInclude(e => e.Product)
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId.Value)
            .OrderBy(c => c.AccountNumber)
            .ToListAsync();

        var currentBill = await db.Bills
            .Include(b => b.Connection)
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId.Value)
            .OrderByDescending(b => b.GeneratedAt)
            .FirstOrDefaultAsync();

        var model = new CustomerDashboardViewModel
        {
            Connections = connections,
            CurrentBill = currentBill,
            OutstandingAmount = await db.Bills.LatestBillPerConnection()
                .Where(b => b.CustomerId == customerId.Value && b.DueAmount > 0)
                .SumAsync(b => (decimal?)b.DueAmount) ?? 0m,
            RecentOrders = await db.Orders
                .Include(o => o.Plan)
                .AsNoTracking()
                .Where(o => o.CustomerId == customerId.Value)
                .OrderByDescending(o => o.SubmittedAt)
                .Take(5)
                .ToListAsync(),
            RecentPayments = await db.Payments
                .Include(p => p.Bill)
                .AsNoTracking()
                .Where(p => p.CustomerId == customerId.Value)
                .OrderByDescending(p => p.PaidAt)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }
}
