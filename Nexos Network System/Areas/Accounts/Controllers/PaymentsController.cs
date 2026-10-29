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
public class PaymentsController(ApplicationDbContext db, IPaymentService paymentService) : Controller
{
    public async Task<IActionResult> Index(string? q, PaymentMethod? method, PaymentStatus? status, int page = 1)
    {
        ViewData["PanelName"] = "Accounts Panel";
        ViewData["q"] = q;
        ViewData["method"] = method;
        ViewData["status"] = status;

        var query = db.Payments
            .Include(p => p.Customer)
            .Include(p => p.Bill)
            .Include(p => p.Connection)
            .Include(p => p.RecordedByEmployee)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                p.ReceiptNumber.Contains(term) ||
                (p.Customer.FirstName + " " + p.Customer.LastName).Contains(term) ||
                p.Connection.AccountNumber.Contains(term));
        }

        if (method.HasValue)
        {
            query = query.Where(p => p.Method == method.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var result = await PagedResult<Payment>.CreateAsync(query.OrderByDescending(p => p.PaidAt), page);

        // Customer panel payments wait here until accounts confirms the money actually arrived.
        var pending = db.Payments.Where(p => p.Status == PaymentStatus.PendingVerification);
        ViewBag.PendingCount = await pending.CountAsync();
        ViewBag.PendingAmount = await pending.SumAsync(p => (decimal?)p.Amount) ?? 0m;

        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(int id, string? notes)
    {
        var employee = await User.GetEmployeeAsync(db);
        if (employee is null)
        {
            TempData["Error"] = "Your accounts employee profile could not be resolved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            var payment = await paymentService.VerifySubmittedPaymentAsync(id, employee.Id, notes);
            TempData["Success"] = $"Payment {payment.ReceiptNumber} confirmed. PKR {payment.Amount:N2} was applied to bill {payment.Bill.BillNumber}.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var employee = await User.GetEmployeeAsync(db);
        if (employee is null)
        {
            TempData["Error"] = "Your accounts employee profile could not be resolved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await paymentService.RejectSubmittedPaymentAsync(id, employee.Id, reason ?? string.Empty);
            TempData["Success"] = "Payment rejected. The bill keeps its outstanding balance.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Accounts Panel";

        var payment = await db.Payments
            .Include(p => p.Customer)
            .Include(p => p.Connection).ThenInclude(c => c.Plan)
            .Include(p => p.Bill)
            .Include(p => p.RecordedByEmployee)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment is null)
        {
            return NotFound();
        }

        return View(payment);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? billId)
    {
        ViewData["PanelName"] = "Accounts Panel";
        await LoadBillsAsync();

        var model = new PaymentRecordViewModel();
        if (billId.HasValue)
        {
            var bill = await db.Bills.Include(b => b.Customer).AsNoTracking().FirstOrDefaultAsync(b => b.Id == billId.Value);
            if (bill is not null)
            {
                model.BillId = bill.Id;
                model.BillNumber = bill.BillNumber;
                model.CustomerName = $"{bill.Customer.FirstName} {bill.Customer.LastName}";
                model.DueAmount = bill.DueAmount;
                model.Amount = bill.DueAmount;
                await ApplyBillDefaultsAsync(model);
            }
        }

        return View(model);
    }

    /// <summary>
    /// Everything the collect form needs for the chosen bill: the card shown under the dropdown,
    /// the amount to collect, and the payment method/reference pre-filled from what is already in
    /// the database so accounts does not have to type them in.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> BillSummary(int id)
    {
        var bill = await db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection).ThenInclude(c => c.Plan)
            .Include(b => b.Connection).ThenInclude(c => c.City)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bill is null)
        {
            return NotFound();
        }

        var (method, reference) = await GetPaymentDefaultsAsync(bill);

        return Json(new
        {
            id = bill.Id,
            billNumber = bill.BillNumber,
            customerName = $"{bill.Customer.FirstName} {bill.Customer.LastName}",
            accountNumber = bill.Connection.AccountNumber,
            planName = bill.Connection.Plan?.Name,
            cityName = bill.Connection.City?.Name,
            statusText = bill.Status.ToString(),
            statusCss = StatusBadges.PillCssClass(bill.Status),
            periodStart = bill.BillingPeriodStart.ToString("dd MMM yyyy"),
            periodEnd = bill.BillingPeriodEnd.ToString("dd MMM yyyy"),
            dueDate = bill.DueDate.ToString("dd MMM yyyy"),
            totalAmount = bill.TotalAmount,
            paidAmount = bill.PaidAmount,
            dueAmount = bill.DueAmount,
            previousBalance = bill.PreviousBalance,
            method = (int)method,
            methodName = method.ToString(),
            reference,
            detailsUrl = Url.Action(nameof(BillsController.Details), "Bills", new { area = "Accounts", id = bill.Id })
        });
    }

    /// <summary>
    /// Payment method and reference taken from the database: the method last recorded for this
    /// bill (cash when it is the first collection) and the next free reference for it.
    /// </summary>
    private async Task<(PaymentMethod Method, string Reference)> GetPaymentDefaultsAsync(Bill bill)
    {
        var lastMethod = await db.Payments
            .AsNoTracking()
            .Where(p => p.BillId == bill.Id && p.Status != PaymentStatus.Cancelled)
            .OrderByDescending(p => p.PaidAt)
            .ThenByDescending(p => p.Id)
            .Select(p => (PaymentMethod?)p.Method)
            .FirstOrDefaultAsync();

        var referenceCount = await db.Payments.CountAsync(p => p.BillId == bill.Id);

        return (lastMethod ?? PaymentMethod.Cash, $"REF-{bill.BillNumber}-{referenceCount + 1}");
    }

    private async Task ApplyBillDefaultsAsync(PaymentRecordViewModel model)
    {
        var bill = await db.Bills.AsNoTracking().FirstOrDefaultAsync(b => b.Id == model.BillId);
        if (bill is null)
        {
            return;
        }

        var (method, reference) = await GetPaymentDefaultsAsync(bill);
        model.Method = method;
        model.PaymentReference = reference;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentRecordViewModel model)
    {
        ViewData["PanelName"] = "Accounts Panel";

        if (!ModelState.IsValid)
        {
            await LoadBillsAsync();
            await FillBillDetailsAsync(model);
            return View(model);
        }

        var employee = await User.GetEmployeeAsync(db);

        try
        {
            var payment = await paymentService.RecordPaymentAsync(model, employee?.Id);
            TempData["Success"] = $"Payment recorded. Receipt number {payment.ReceiptNumber} for PKR {payment.Amount:N2}.";
            return RedirectToAction(nameof(Details), new { id = payment.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadBillsAsync();
            await FillBillDetailsAsync(model);
            return View(model);
        }
    }

    private async Task FillBillDetailsAsync(PaymentRecordViewModel model)
    {
        var bill = await db.Bills.Include(b => b.Customer).AsNoTracking().FirstOrDefaultAsync(b => b.Id == model.BillId);
        if (bill is not null)
        {
            model.BillNumber = bill.BillNumber;
            model.CustomerName = $"{bill.Customer.FirstName} {bill.Customer.LastName}";
            model.DueAmount = bill.DueAmount;

            if (string.IsNullOrWhiteSpace(model.PaymentReference))
            {
                await ApplyBillDefaultsAsync(model);
            }
        }
    }

    private async Task LoadBillsAsync()
    {
        var bills = await db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection)
            .AsNoTracking()
            .LatestBillPerConnection()
            .Where(b => b.DueAmount > 0 && b.Status != BillStatus.Cancelled)
            .OrderBy(b => b.DueDate)
            .Select(b => new
            {
                b.Id,
                Label = b.BillNumber + " - " + b.Customer.FirstName + " " + b.Customer.LastName +
                        " (Due PKR " + b.DueAmount + ")"
            })
            .ToListAsync();

        ViewBag.Bills = new SelectList(bills, "Id", "Label");
    }
}
