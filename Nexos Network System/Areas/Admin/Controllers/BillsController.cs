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

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BillsController(ApplicationDbContext db, IBillingService billingService) : Controller
{
    public async Task<IActionResult> Index(string? q, BillStatus? status, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";
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
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

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

        return View(bill);
    }

    [HttpGet]
    public async Task<IActionResult> Generate()
    {
        ViewData["PanelName"] = "Admin Panel";
        await LoadConnectionsAsync();
        return View(new BillGenerateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(BillGenerateViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

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
            TempData["Success"] = $"Bill {bill.BillNumber} was generated with a total of PKR {bill.TotalAmount:N2} (including 12.24% service tax).";
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
