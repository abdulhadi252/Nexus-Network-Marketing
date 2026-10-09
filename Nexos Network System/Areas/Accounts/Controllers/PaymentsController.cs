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
    public async Task<IActionResult> Index(string? q, PaymentMethod? method, int page = 1)
    {
        ViewData["PanelName"] = "Accounts Panel";
        ViewData["q"] = q;
        ViewData["method"] = method;

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

        var result = await PagedResult<Payment>.CreateAsync(query.OrderByDescending(p => p.PaidAt), page);
        return View(result);
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
            }
        }

        return View(model);
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
