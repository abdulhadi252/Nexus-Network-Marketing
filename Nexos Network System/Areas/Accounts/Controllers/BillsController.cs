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

namespace Nexos_Network_System.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize(Roles = "Accounts")]
public class BillsController(ApplicationDbContext db, IBillingService billingService) : Controller
{
    public async Task<IActionResult> Index(string? q, BillStatus? status, int page = 1)
    {
        ViewData["PanelName"] = "Accounts Panel";
        ViewData["q"] = q;
        ViewData["status"] = status;

        var query = db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(b =>
                b.BillNumber.Contains(term) ||
                b.Connection.AccountNumber.Contains(term) ||
                (b.Customer.FirstName + " " + b.Customer.LastName).Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        var result = await PagedResult<Bill>.CreateAsync(query.OrderByDescending(b => b.GeneratedAt), page);
        ViewBag.TotalOutstanding = await db.Bills.LatestBillPerConnection()
            .Where(b => b.DueAmount > 0)
            .SumAsync(b => (decimal?)b.DueAmount) ?? 0m;
        ViewBag.PayableBillIds = (await db.Bills.LatestBillPerConnection()
                .Where(b => b.DueAmount > 0)
                .Select(b => b.Id)
                .ToListAsync())
            .ToHashSet();
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Accounts Panel";

        var bill = await db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection).ThenInclude(c => c.Plan)
            .Include(b => b.Items)
            .Include(b => b.Payments).ThenInclude(p => p.RecordedByEmployee)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bill is null)
        {
            return NotFound();
        }

        ViewBag.CanRecordPayment = await db.Bills.LatestBillPerConnection()
            .AnyAsync(candidate => candidate.Id == bill.Id && candidate.DueAmount > 0);

        // The notification card reports what the customer has already been told about this bill.
        ViewBag.LastNotification = await db.CustomerNotifications
            .AsNoTracking()
            .Where(n => n.BillId == bill.Id)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .FirstOrDefaultAsync();

        ViewBag.NotificationCount = await db.CustomerNotifications.CountAsync(n => n.BillId == bill.Id);

        return View(bill);
    }

    /// <summary>
    /// Puts a bill-ready message into the bill owner's own customer panel (bell + Notifications
    /// page). Only that customer sees it, and the page links straight to the payment form.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NotifyCustomer(int id, string? message)
    {
        ViewData["PanelName"] = "Accounts Panel";

        var bill = await db.Bills
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bill is null)
        {
            return NotFound();
        }

        if (message?.Length > 1000)
        {
            TempData["Error"] = "The extra message cannot exceed 1,000 characters.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var employee = await User.GetEmployeeAsync(db);

        db.CustomerNotifications.Add(new CustomerNotification
        {
            CustomerId = bill.CustomerId,
            BillId = bill.Id,
            Title = $"Bill {bill.BillNumber} is ready - PKR {bill.DueAmount:N2} due",
            Message = BillNotificationBody(bill, message),
            Amount = bill.DueAmount,
            CreatedByEmployeeId = employee?.Id
        });
        await db.SaveChangesAsync();

        TempData["Success"] = $"Bill notification sent to {bill.Customer.FirstName} {bill.Customer.LastName}'s customer panel.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static string BillNotificationBody(Bill bill, string? message)
    {
        var lines = new List<string>
        {
            $"Dear {bill.Customer.FirstName} {bill.Customer.LastName},".Trim(),
            string.Empty,
            $"Your bill {bill.BillNumber} for {bill.BillingPeriodStart:dd MMM yyyy} - {bill.BillingPeriodEnd:dd MMM yyyy} has been generated.",
            $"Amount due: PKR {bill.DueAmount:N2}",
            $"Due date: {bill.DueDate:dd MMM yyyy}",
            string.Empty,
            "You can pay this bill from your customer panel: open My Bills and press Pay now. " +
            "Once the payment is submitted, our accounts team confirms it and the bill updates automatically."
        };

        if (!string.IsNullOrWhiteSpace(message))
        {
            lines.Add(string.Empty);
            lines.Add("Message from our accounts team:");
            lines.Add(message.Trim());
        }

        return string.Join(Environment.NewLine, lines);
    }

    [HttpGet]
    public async Task<IActionResult> Generate(int? connectionId)
    {
        ViewData["PanelName"] = "Accounts Panel";
        await LoadConnectionsAsync();
        return View(new BillGenerateViewModel { ConnectionId = connectionId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(BillGenerateViewModel model)
    {
        ViewData["PanelName"] = "Accounts Panel";

        if (model.BillingPeriodEnd < model.BillingPeriodStart)
        {
            ModelState.AddModelError(nameof(model.BillingPeriodEnd), "The period end date must be after the start date.");
        }

        if (await db.Bills.AnyAsync(b => b.ConnectionId == model.ConnectionId
                                         && b.BillingPeriodStart == model.BillingPeriodStart
                                         && b.BillingPeriodEnd == model.BillingPeriodEnd))
        {
            ModelState.AddModelError(string.Empty, "A bill already exists for this connection and billing period.");
        }

        if (!ModelState.IsValid)
        {
            await LoadConnectionsAsync();
            return View(model);
        }

        try
        {
            var bill = await billingService.GenerateBillAsync(model);
            TempData["Success"] = $"Bill {bill.BillNumber} generated. Total PKR {bill.TotalAmount:N2} including 12.24% service tax.";
            return RedirectToAction(nameof(Details), new { id = bill.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadConnectionsAsync();
            return View(model);
        }
    }

    private async Task LoadConnectionsAsync()
    {
        var connections = await db.Connections
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .AsNoTracking()
            .Where(c => c.Status == ConnectionStatus.Active || c.Status == ConnectionStatus.TemporarilyInactive)
            .OrderBy(c => c.AccountNumber)
            .Select(c => new
            {
                c.Id,
                Label = c.AccountNumber + " - " + c.Customer.FirstName + " " + c.Customer.LastName + " (" + c.Plan.Name + ")"
            })
            .ToListAsync();

        ViewBag.Connections = new SelectList(connections, "Id", "Label");
    }
}
