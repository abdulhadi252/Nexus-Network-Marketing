using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize(Roles = "Accounts")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Accounts Panel";

        var model = new DashboardStatsViewModel
        {
            TotalBills = await db.Bills.CountAsync(),
            PaidBills = await db.Bills.LatestBillPerConnection().CountAsync(b => b.Status == BillStatus.Paid),
            PartiallyPaidBills = await db.Bills.LatestBillPerConnection().CountAsync(b => b.Status == BillStatus.PartiallyPaid),
            DueBills = await db.Bills.LatestBillPerConnection().CountAsync(b => b.Status == BillStatus.Unpaid),
            OutstandingBills = await db.Bills.LatestBillPerConnection().CountAsync(b => b.DueAmount > 0),
            OutstandingAmount = await db.Bills.LatestBillPerConnection()
                .Where(b => b.DueAmount > 0).SumAsync(b => (decimal?)b.DueAmount) ?? 0m,
            PaymentsReceived = await db.Payments.Where(p => p.Status == PaymentStatus.Recorded).SumAsync(p => (decimal?)p.Amount) ?? 0m,
            TotalCustomers = await db.Customers.CountAsync(),
            ActiveConnections = await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Active),
            RecentPayments = await db.Payments
                .Include(p => p.Customer)
                .Include(p => p.Bill)
                .OrderByDescending(p => p.PaidAt)
                .Take(8)
                .ToListAsync(),
            DueBillsList = await db.Bills.LatestBillPerConnection()
                .Include(b => b.Customer)
                .Include(b => b.Connection)
                .Where(b => b.DueAmount > 0)
                .OrderBy(b => b.DueDate)
                .Take(10)
                .ToListAsync()
        };

        return View(model);
    }
}
