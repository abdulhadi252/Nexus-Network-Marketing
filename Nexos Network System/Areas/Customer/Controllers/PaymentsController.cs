using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class PaymentsController(ApplicationDbContext db, IPaymentService paymentService) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record.";
            return View(new List<Payment>());
        }

        var payments = await db.Payments
            .Include(p => p.Bill)
            .Include(p => p.Connection)
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId.Value)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();

        return View(payments);
    }

    [HttpGet]
    public async Task<IActionResult> Pay(int? billId)
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record.";
            return RedirectToAction(nameof(Index));
        }

        var model = new CustomerPaymentViewModel
        {
            OpenBills = await LoadOpenBillsAsync(customerId.Value)
        };

        if (billId.HasValue)
        {
            FillFromBill(model, model.OpenBills.FirstOrDefault(b => b.Id == billId.Value));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(CustomerPaymentViewModel model)
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record.";
            return RedirectToAction(nameof(Index));
        }

        var openBills = await LoadOpenBillsAsync(customerId.Value);
        model.OpenBills = openBills;
        FillFromBill(model, openBills.FirstOrDefault(b => b.Id == model.BillId));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var payment = await paymentService.SubmitOnlinePaymentAsync(model, customerId.Value);
            TempData["Success"] = $"Payment of PKR {payment.Amount:N2} submitted. Reference {payment.ReceiptNumber}. " +
                                  "The accounts team will confirm it and your bill will update automatically.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private async Task<IReadOnlyList<Bill>> LoadOpenBillsAsync(int customerId)
    {
        return await db.Bills
            .Include(b => b.Connection)
            .AsNoTracking()
            .LatestBillPerConnection()
            .Where(b => b.CustomerId == customerId && b.DueAmount > 0 && b.Status != BillStatus.Cancelled)
            .OrderBy(b => b.DueDate)
            .ToListAsync();
    }

    private static void FillFromBill(CustomerPaymentViewModel model, Bill? bill)
    {
        if (bill is null)
        {
            return;
        }

        model.BillId = bill.Id;
        model.BillNumber = bill.BillNumber;
        model.AccountNumber = bill.Connection?.AccountNumber ?? string.Empty;
        model.DueAmount = bill.DueAmount;
        if (model.Amount <= 0 || model.Amount == model.DueAmount)
        {
            model.Amount = bill.DueAmount;
        }
    }
}
