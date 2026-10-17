using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PaymentsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(
        string? q,
        PaymentMethod? method,
        PaymentStatus? status,
        DateTime? from,
        DateTime? to,
        int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";

        const int pageSize = 10;

        var query = db.Payments
            .AsNoTracking()
            .Include(p => p.Customer)
            .Include(p => p.Bill)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                p.ReceiptNumber.Contains(term) ||
                p.Bill.BillNumber.Contains(term) ||
                (p.PaymentReference != null && p.PaymentReference.Contains(term)) ||
                p.Customer.FirstName.Contains(term) ||
                p.Customer.LastName.Contains(term));
        }

        if (method.HasValue) query = query.Where(p => p.Method == method.Value);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);

        if (from.HasValue)
        {
            var start = new DateTimeOffset(from.Value.Date, TimeZoneInfo.Local.GetUtcOffset(from.Value.Date));
            query = query.Where(p => p.PaidAt >= start);
        }
        if (to.HasValue)
        {
            var endDay = to.Value.Date.AddDays(1);
            var end = new DateTimeOffset(endDay, TimeZoneInfo.Local.GetUtcOffset(endDay));
            query = query.Where(p => p.PaidAt < end);
        }

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderByDescending(p => p.PaidAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Summary cards: saare payments par (filter ka asar nahi)
        var byStatus = await db.Payments
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Sum = g.Sum(x => x.Amount) })
            .ToListAsync();

        var vm = new PaymentsIndexViewModel
        {
            Payments = items,
            Q = q, Method = method, Status = status, From = from, To = to,
            Page = page, PageSize = pageSize, TotalCount = total,
            AllCount = byStatus.Sum(x => x.Count),
            TotalCollected = byStatus.Where(x => PaymentUi.StatusKind(x.Status) == "success").Sum(x => x.Sum),
            SuccessCount = byStatus.Where(x => PaymentUi.StatusKind(x.Status) == "success").Sum(x => x.Count),
            PendingCount = byStatus.Where(x => PaymentUi.StatusKind(x.Status) == "pending").Sum(x => x.Count),
            PendingAmount = byStatus.Where(x => PaymentUi.StatusKind(x.Status) == "pending").Sum(x => x.Sum),
            RefundedAmount = byStatus.Where(x => PaymentUi.StatusKind(x.Status) == "refunded").Sum(x => x.Sum),            SuccessRate = total > 0 ? (double)byStatus.Where(x => PaymentUi.StatusKind(x.Status) == "success").Sum(x => x.Count) / total * 100 : 0
        };

        return View(vm);
    }

    /// <summary>
    /// Streams the filtered payment list as CSV; mirrors every Index filter so the
    /// export always matches what is on screen. No paging on purpose — one row per
    /// matching payment.
    /// </summary>
    public async Task<IActionResult> Export(
        string? q,
        PaymentMethod? method,
        PaymentStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        ViewData["PanelName"] = "Admin Panel";

        var query = db.Payments
            .Include(p => p.Customer)
            .Include(p => p.Bill)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                p.ReceiptNumber.Contains(term) ||
                p.Bill.BillNumber.Contains(term) ||
                (p.PaymentReference != null && p.PaymentReference.Contains(term)) ||
                p.Customer.FirstName.Contains(term) ||
                p.Customer.LastName.Contains(term));
        }

        if (method.HasValue)
        {
            query = query.Where(p => p.Method == method.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (from.HasValue)
        {
            var start = new DateTimeOffset(from.Value.Date, TimeZoneInfo.Local.GetUtcOffset(from.Value.Date));
            query = query.Where(p => p.PaidAt >= start);
        }
        if (to.HasValue)
        {
            var endDay = to.Value.Date.AddDays(1);
            var end = new DateTimeOffset(endDay, TimeZoneInfo.Local.GetUtcOffset(endDay));
            query = query.Where(p => p.PaidAt < end);
        }

        var rows = await query
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new
            {
                p.ReceiptNumber,
                Customer = p.Customer.FirstName + " " + p.Customer.LastName,
                Bill = p.Bill.BillNumber,
                Account = p.Connection.AccountNumber,
                Method = p.Method.ToString(),
                Status = p.Status.ToString(),
                p.PaidAt,
                p.Amount
            })
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("Receipt,Customer,Bill,Account,Method,Status,Date,Amount (PKR)");
        foreach (var row in rows)
        {
            builder.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"{row.ReceiptNumber},{row.Customer},{row.Bill},{row.Account},{row.Method},{row.Status},{row.PaidAt.ToLocalTime():yyyy-MM-dd HH:mm},{row.Amount:F2}"));
        }

        return File(
            Encoding.UTF8.GetBytes(builder.ToString()),
            "text/csv",
            $"payments-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

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
}
