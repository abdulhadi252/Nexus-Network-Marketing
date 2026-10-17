using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class VendorsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";
        ViewData["q"] = q;

        var query = db.Vendors.Include(v => v.Products).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(v => v.Name.Contains(term) || v.ContactNumber.Contains(term));
        }

        var result = await PagedResult<Vendor>.CreateAsync(query.OrderBy(v => v.Name), page);

        // Sparkline KPI tiles above the directory, Messages-page style.
        var vendors = await db.Vendors.AsNoTracking()
            .Include(v => v.Products)
            .Include(v => v.EquipmentItems)
            .ToListAsync();

        ViewData["KpiTotal"] = AdminKpi.Static(vendors.Count, "vendors on record");
        ViewData["KpiActive"] = AdminKpi.Static(vendors.Count(v => v.IsActive), "active vendors");
        ViewData["KpiProducts"] = AdminKpi.Static(vendors.Sum(v => v.Products.Count), "catalogue products");
        ViewData["KpiEquipment"] = AdminKpi.Static(vendors.Sum(v => v.EquipmentItems.Count), "equipment items");

        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var vendor = await db.Vendors
            .Include(v => v.Products)
            .Include(v => v.EquipmentItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vendor is null)
        {
            return NotFound();
        }

        return View(vendor);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["PanelName"] = "Admin Panel";
        return View(new VendorFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VendorFormViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        db.Vendors.Add(new Vendor
        {
            Name = model.Name.Trim(),
            ContactNumber = model.ContactNumber.Trim(),
            Email = model.Email?.Trim(),
            Address = model.Address?.Trim(),
            IsActive = model.IsActive
        });
        await db.SaveChangesAsync();

        TempData["Success"] = "Vendor was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var vendor = await db.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
        if (vendor is null)
        {
            return NotFound();
        }

        return View(new VendorFormViewModel
        {
            Id = vendor.Id,
            Name = vendor.Name,
            ContactNumber = vendor.ContactNumber,
            Email = vendor.Email,
            Address = vendor.Address,
            IsActive = vendor.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VendorFormViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        vendor.Name = model.Name.Trim();
        vendor.ContactNumber = model.ContactNumber.Trim();
        vendor.Email = model.Email?.Trim();
        vendor.Address = model.Address?.Trim();
        vendor.IsActive = model.IsActive;
        await db.SaveChangesAsync();

        TempData["Success"] = "Vendor details were updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var vendor = await db.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
        if (vendor is null)
        {
            return NotFound();
        }

        return View(vendor);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor is null)
        {
            return NotFound();
        }

        var hasDependencies = await db.Products.AnyAsync(p => p.VendorId == id) || await db.Equipment.AnyAsync(e => e.VendorId == id);
        if (hasDependencies)
        {
            vendor.IsActive = false;
            await db.SaveChangesAsync();
            TempData["Success"] = "This vendor has linked products or equipment, so it was deactivated instead of deleted.";
            return RedirectToAction(nameof(Index));
        }

        db.Vendors.Remove(vendor);
        await db.SaveChangesAsync();
        TempData["Success"] = "Vendor was deleted.";
        return RedirectToAction(nameof(Index));
    }
}
