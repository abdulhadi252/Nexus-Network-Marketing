using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class RetailShopsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";
        ViewData["q"] = q;

        var query = db.RetailShops.Include(s => s.City).Include(s => s.Employees).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s => s.Name.Contains(term) || s.ContactNumber.Contains(term));
        }

        var result = await PagedResult<RetailShop>.CreateAsync(query.OrderBy(s => s.Name), page);

        // Sparkline KPI tiles above the directory, Messages-page style.
        var shops = await db.RetailShops.AsNoTracking()
            .Include(s => s.Employees)
            .Include(s => s.Orders)
            .ToListAsync();

        ViewData["KpiTotal"] = AdminKpi.Static(shops.Count, "retail shops");
        ViewData["KpiActive"] = AdminKpi.Static(shops.Count(s => s.IsActive), "active branches");
        ViewData["KpiEmployees"] = AdminKpi.Static(shops.Sum(s => s.Employees.Count), "branch staff");
        ViewData["KpiOrders"] = AdminKpi.Static(shops.Sum(s => s.Orders.Count), "orders booked");

        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var shop = await db.RetailShops
            .Include(s => s.City)
            .Include(s => s.Employees)
            .Include(s => s.Orders)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shop is null)
        {
            return NotFound();
        }

        return View(shop);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["PanelName"] = "Admin Panel";
        await LoadCitiesAsync();
        return View(new RetailShopFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RetailShopFormViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        if (!ModelState.IsValid)
        {
            await LoadCitiesAsync();
            return View(model);
        }

        db.RetailShops.Add(new RetailShop
        {
            Name = model.Name.Trim(),
            AddressLine1 = model.AddressLine1.Trim(),
            AddressLine2 = model.AddressLine2?.Trim(),
            ContactNumber = model.ContactNumber.Trim(),
            CityId = model.CityId,
            IsActive = model.IsActive
        });
        await db.SaveChangesAsync();

        TempData["Success"] = "Retail shop was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var shop = await db.RetailShops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (shop is null)
        {
            return NotFound();
        }

        await LoadCitiesAsync();
        return View(new RetailShopFormViewModel
        {
            Id = shop.Id,
            Name = shop.Name,
            AddressLine1 = shop.AddressLine1,
            AddressLine2 = shop.AddressLine2,
            ContactNumber = shop.ContactNumber,
            CityId = shop.CityId,
            IsActive = shop.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RetailShopFormViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        var shop = await db.RetailShops.FirstOrDefaultAsync(s => s.Id == id);
        if (shop is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadCitiesAsync();
            return View(model);
        }

        shop.Name = model.Name.Trim();
        shop.AddressLine1 = model.AddressLine1.Trim();
        shop.AddressLine2 = model.AddressLine2?.Trim();
        shop.ContactNumber = model.ContactNumber.Trim();
        shop.CityId = model.CityId;
        shop.IsActive = model.IsActive;
        await db.SaveChangesAsync();

        TempData["Success"] = "Retail shop details were updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var shop = await db.RetailShops.Include(s => s.City).AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (shop is null)
        {
            return NotFound();
        }

        return View(shop);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var shop = await db.RetailShops.FirstOrDefaultAsync(s => s.Id == id);
        if (shop is null)
        {
            return NotFound();
        }

        var hasDependencies = await db.Employees.AnyAsync(e => e.RetailShopId == id) || await db.Orders.AnyAsync(o => o.RetailShopId == id);
        if (hasDependencies)
        {
            shop.IsActive = false;
            await db.SaveChangesAsync();
            TempData["Success"] = "This shop has linked employees or orders, so it was deactivated instead of deleted.";
            return RedirectToAction(nameof(Index));
        }

        db.RetailShops.Remove(shop);
        await db.SaveChangesAsync();
        TempData["Success"] = "Retail shop was deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCitiesAsync()
    {
        ViewBag.Cities = new SelectList(
            await db.Cities.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
            "Id", "Name");
    }
}
