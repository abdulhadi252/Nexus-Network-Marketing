using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

using CustomerModel = Nexos_Network_System.Models.Customer;

namespace Nexos_Network_System.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize(Roles = "Accounts")]
public class CustomersController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        ViewData["PanelName"] = "Accounts Panel";
        ViewData["q"] = q;

        var query = db.Customers
            .Include(c => c.Bills)
            .Include(c => c.Connections)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(c =>
                c.FirstName.Contains(term) ||
                c.LastName.Contains(term) ||
                c.Email.Contains(term) ||
                c.PrimaryContactNumber.Contains(term));
        }

        var result = await PagedResult<CustomerModel>.CreateAsync(query.OrderBy(c => c.LastName), page);
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Accounts Panel";

        var customer = await db.Customers
            .Include(c => c.Connections).ThenInclude(cn => cn.Plan)
            .Include(c => c.Bills)
            .Include(c => c.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null)
        {
            return NotFound();
        }

        return View(customer);
    }
}
