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
    /// <summary>Months of history behind each summary tile's sparkline.</summary>
    private const int TrendMonths = 6;

    /// <summary>Bills generated within this many days count towards the "this week" figure.</summary>
    private const int RecentWindowDays = 7;

    public async Task<IActionResult> Index(string? q, BillStatus? status, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";

        var query = db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection).ThenInclude(c => c.Plan)
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

        var bills = await PagedResult<Bill>.CreateAsync(query.OrderByDescending(b => b.GeneratedAt), page);

        var now = DateTimeOffset.UtcNow;
        var weekAgo = now.AddDays(-RecentWindowDays);
        var monthStart = MonthlySeries.StartOfMonth(now);
        var windowStart = monthStart.AddMonths(-(TrendMonths - 1));

        // ------------------------------------------------------------------
        // Outstanding: the latest bill of each connection that still owes money.
        // ------------------------------------------------------------------
        var outstanding = await SumOutstandingAsync(null);
        var outstandingLastWeek = await SumOutstandingAsync(weekAgo);

        // ------------------------------------------------------------------
        // Only the columns the tile series are built from, and only for the
        // visible window; anything older is covered by the baseline counts.
        // ------------------------------------------------------------------
        var windowRows = await db.Bills
            .Where(b => b.Status != BillStatus.Cancelled && b.GeneratedAt >= windowStart)
            .Select(b => new { b.GeneratedAt, b.ServiceTaxAmount, b.TotalAmount })
            .AsNoTracking()
            .ToListAsync();

        var billsBeforeWindow = await db.Bills
            .CountAsync(b => b.Status != BillStatus.Cancelled && b.GeneratedAt < windowStart);

        var monthlyCounts = MonthlySeries.Counts(
            windowRows.Select(row => (DateTimeOffset?)row.GeneratedAt),
            windowStart,
            TrendMonths);

        var monthlyTaxRates = MonthlyTaxRates(
            windowRows.Select(row => (
                row.GeneratedAt,
                (double)row.ServiceTaxAmount,
                (double)(row.TotalAmount - row.ServiceTaxAmount))),
            windowStart,
            TrendMonths,
            (double)IBillingService.ServiceTaxRate * 100d);

        // Outstanding measured at the start of each month in the window, finishing on today's
        // figure so the sparkline ends on the value shown on the tile above it.
        var monthlyOutstanding = new List<double>();
        for (var offset = TrendMonths - 1; offset >= 1; offset--)
        {
            monthlyOutstanding.Add(await SumOutstandingAsync(monthStart.AddMonths(-offset)));
        }

        monthlyOutstanding.Add(outstanding);

        var billsThisWeek = windowRows.Count(row => row.GeneratedAt >= weekAgo);
        var billsThisMonth = windowRows.Count(row => row.GeneratedAt >= monthStart);
        var billsLastMonth = windowRows.Count(row =>
            row.GeneratedAt >= monthStart.AddMonths(-1) && row.GeneratedAt < monthStart);

        var outstandingChange = outstanding - outstandingLastWeek;
        var monthChange = billsThisMonth - billsLastMonth;
        var taxRate = (double)IBillingService.ServiceTaxRate * 100d;

        var model = new BillIndexViewModel
        {
            Bills = bills,
            Query = q,
            Status = status,

            TotalBills = new BillMetric
            {
                Value = billsBeforeWindow + monthlyCounts.Sum(),
                Trend = MonthlySeries.Cumulative(monthlyCounts, billsBeforeWindow)
                    .Select(value => (double)value)
                    .ToList(),
                Foot = $"{billsThisWeek} this week",
                ShowFootArrow = true,
                FootRises = true,
                FootIsBad = false
            },
            Outstanding = new BillMetric
            {
                Value = outstanding,
                Trend = monthlyOutstanding,
                Foot = $"PKR {Math.Abs(outstandingChange):N2} this week",
                ShowFootArrow = true,
                FootRises = outstandingChange >= 0,
                FootIsBad = outstandingChange >= 0
            },
            ServiceTax = new BillMetric
            {
                Value = taxRate,
                Trend = monthlyTaxRates,
                Foot = "of total bill amount",
                ShowFootArrow = false
            },
            ThisMonth = new BillMetric
            {
                Value = billsThisMonth,
                Trend = monthlyCounts.Select(value => (double)value).ToList(),
                Foot = $"{Math.Abs(monthChange)} vs. last month",
                ShowFootArrow = true,
                FootRises = monthChange >= 0,
                FootIsBad = monthChange < 0
            }
        };

        return View(model);
    }

    /// <summary>Total still owing across the latest bill of each connection at the given moment.</summary>
    private async Task<double> SumOutstandingAsync(DateTimeOffset? asOf) =>
        (double)(await db.Bills.LatestBillPerConnection(asOf)
            .Where(b => b.DueAmount > 0)
            .SumAsync(b => (decimal?)b.DueAmount) ?? 0m);

    /// <summary>
    /// Effective service tax rate per month: the tax billed divided by the taxable base that
    /// produced it, so the series tracks the rate customers were actually charged. Months without
    /// billing carry the previous rate forward rather than dropping to zero.
    /// </summary>
    private static List<double> MonthlyTaxRates(
        IEnumerable<(DateTimeOffset Moment, double Tax, double Taxable)> rows,
        DateTimeOffset windowStart,
        int months,
        double fallback)
    {
        var tax = new double[months];
        var taxable = new double[months];

        foreach (var (moment, rowTax, rowTaxable) in rows)
        {
            var index = MonthlySeries.Index(moment, windowStart);
            if (index >= 0 && index < months)
            {
                tax[index] += rowTax;
                taxable[index] += rowTaxable;
            }
        }

        var rates = new List<double>(months);
        var lastRate = fallback;

        for (var index = 0; index < months; index++)
        {
            if (taxable[index] > 0)
            {
                lastRate = tax[index] / taxable[index] * 100d;
            }

            rates.Add(lastRate);
        }

        return rates;
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
