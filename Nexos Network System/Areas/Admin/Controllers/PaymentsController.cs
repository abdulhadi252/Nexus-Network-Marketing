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
    public async Task<IActionResult> Index(string? q, PaymentMethod? method, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";
        ViewData["q"] = q;
        ViewData["method"] = method;

        var query = db.Payments
            .Include(p => p.Customer)
            .Include(p => p.Bill)
            .Include(p => p.Connection)
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
