using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize(Roles = "Accounts")]
public class DuesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, bool overdueOnly = false, int page = 1)
    {
        ViewData["PanelName"] = "Accounts Panel";
        ViewData["q"] = q;
        ViewData["overdueOnly"] = overdueOnly;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection)
            .AsNoTracking()
            .LatestBillPerConnection()
            .Where(b => b.DueAmount > 0 && b.Status != BillStatus.Cancelled);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(b =>
                b.Connection.AccountNumber.Contains(term) ||
                (b.Customer.FirstName + " " + b.Customer.LastName).Contains(term));
        }

        if (overdueOnly)
        {
            query = query.Where(b => b.DueDate < today);
        }

        var paged = await PagedResult<Bill>.CreateAsync(query.OrderBy(b => b.DueDate), page);

        ViewBag.TotalOutstanding = await db.Bills.LatestBillPerConnection()
            .Where(b => b.DueAmount > 0 && b.Status != BillStatus.Cancelled)
            .SumAsync(b => (decimal?)b.DueAmount) ?? 0m;

        ViewBag.OverdueCount = await db.Bills.LatestBillPerConnection()
            .CountAsync(b => b.DueAmount > 0 && b.Status != BillStatus.Cancelled && b.DueDate < today);

        ViewBag.OverdueAmount = await db.Bills.LatestBillPerConnection()
            .Where(b => b.DueAmount > 0 && b.Status != BillStatus.Cancelled && b.DueDate < today)
            .SumAsync(b => (decimal?)b.DueAmount) ?? 0m;

        // Every customer with an unpaid bill, so the reminder form can reach all of them
        // (not only the ones on the current page of the list).
        ViewBag.ReminderBills = await db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection)
            .AsNoTracking()
            .LatestBillPerConnection()
            .Where(b => b.DueAmount > 0 && b.Status != BillStatus.Cancelled)
            .OrderBy(b => b.DueDate)
            .ThenBy(b => b.Customer.FirstName)
            .ToListAsync();

        return View(paged);
    }

    /// <summary>
    /// Sends a payment reminder for one unpaid bill into that customer's own panel (bell +
    /// Notifications page). The bill details are read from the database, so a tampered request
    /// can never produce a wrong amount.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendReminder(int billId, string? message)
    {
        ViewData["PanelName"] = "Accounts Panel";

        var bill = await db.Bills
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == billId);

        if (bill is null)
        {
            TempData["Error"] = "That bill no longer exists.";
            return RedirectToAction(nameof(Index));
        }

        if (message?.Length > 1000)
        {
            TempData["Error"] = "The note cannot exceed 1,000 characters.";
            return RedirectToAction(nameof(Index));
        }

        // Only the live bill of a connection can be reminded: an older bill that a newer one
        // replaced would show a due amount the customer can no longer pay.
        var isLiveDue = await db.Bills.LatestBillPerConnection()
            .AnyAsync(b => b.Id == bill.Id && b.DueAmount > 0);

        if (!isLiveDue)
        {
            TempData["Error"] = "This bill has no outstanding due on the latest bill for that connection, so no reminder was sent.";
            return RedirectToAction(nameof(Index));
        }

        var employee = await User.GetEmployeeAsync(db);

        db.CustomerNotifications.Add(new CustomerNotification
        {
            CustomerId = bill.CustomerId,
            BillId = bill.Id,
            Title = $"Payment reminder - bill {bill.BillNumber}, PKR {bill.DueAmount:N2} still due",
            Message = ReminderBody(bill, message),
            Amount = bill.DueAmount,
            CreatedByEmployeeId = employee?.Id
        });
        await db.SaveChangesAsync();

        TempData["Success"] = $"Payment reminder sent to {bill.Customer.FirstName} {bill.Customer.LastName}'s customer panel.";
        return RedirectToAction(nameof(Index));
    }

    private static string ReminderBody(Bill bill, string? note)
    {
        var lines = new List<string>
        {
            $"Dear {bill.Customer.FirstName} {bill.Customer.LastName},",
            string.Empty,
            $"Our records show that bill {bill.BillNumber} for {bill.BillingPeriodStart:dd MMM yyyy} - {bill.BillingPeriodEnd:dd MMM yyyy} is still unpaid.",
            $"Outstanding amount: PKR {bill.DueAmount:N2}",
            $"Due date: {bill.DueDate:dd MMM yyyy}",
            string.Empty,
            "Please pay it from your customer panel: open My Bills and press Pay now. Once the payment is submitted, our accounts team confirms it and the bill updates automatically."
        };

        if (!string.IsNullOrWhiteSpace(note))
        {
            lines.Add(string.Empty);
            lines.Add("Note from our accounts team:");
            lines.Add(note.Trim());
        }

        return string.Join(Environment.NewLine, lines);
    }
}
