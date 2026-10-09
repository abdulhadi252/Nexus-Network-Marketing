using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Admin Panel";

        var model = new DashboardStatsViewModel
        {
            TotalCustomers = await db.Customers.CountAsync(),
            TotalEmployees = await db.Employees.CountAsync(),
            TotalRetailShops = await db.RetailShops.CountAsync(),
            TotalVendors = await db.Vendors.CountAsync(),
            TotalProducts = await db.Products.CountAsync(),
            TotalPlans = await db.Plans.CountAsync(),
            TotalOrders = await db.Orders.CountAsync(),
            PendingOrders = await db.Orders.CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending),
            ActiveConnections = await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Active),
            PendingConnections = await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Pending),
            TotalBills = await db.Bills.CountAsync(),
            DueBills = await db.Bills.LatestBillPerConnection().CountAsync(b => b.DueAmount > 0),
            OutstandingBills = await db.Bills.LatestBillPerConnection().CountAsync(b => b.DueAmount > 0),
            OutstandingAmount = await db.Bills.LatestBillPerConnection()
                .Where(b => b.DueAmount > 0).SumAsync(b => (decimal?)b.DueAmount) ?? 0m,
            PaymentsReceived = await db.Payments.Where(p => p.Status == PaymentStatus.Recorded).SumAsync(p => (decimal?)p.Amount) ?? 0m,
            LowStockItems = await db.StockItems.CountAsync(s => s.QuantityOnHand <= s.ReorderLevel),
            OpenFeedback = await db.FeedbackEntries.CountAsync(f => !f.IsReviewed),
            RecentOrders = await db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Plan)
                .OrderByDescending(o => o.SubmittedAt)
                .Take(6)
                .ToListAsync(),
            RecentPayments = await db.Payments
                .Include(p => p.Customer)
                .Include(p => p.Bill)
                .OrderByDescending(p => p.PaidAt)
                .Take(6)
                .ToListAsync(),
            LowStock = await db.StockItems
                .Include(s => s.Product)
                .Where(s => s.QuantityOnHand <= s.ReorderLevel)
                .OrderBy(s => s.QuantityOnHand)
                .Take(6)
                .ToListAsync()
        };

        return View(model);
    }
}
