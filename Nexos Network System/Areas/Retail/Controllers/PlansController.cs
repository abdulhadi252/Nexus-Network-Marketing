using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;

namespace Nexos_Network_System.Areas.Retail.Controllers;

[Area("Retail")]
[Authorize(Roles = "Retail")]
public class PlansController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        ViewData["PanelName"] = "Retail Panel";
        ViewData["q"] = q;

        var query = db.Plans.AsNoTracking().Where(p => p.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Name.Contains(term));
        }

        var result = await PagedResult<Plan>.CreateAsync(query.OrderBy(p => p.ConnectionType).ThenBy(p => p.RecurringCharge), page);
        return View(result);
    }
}
