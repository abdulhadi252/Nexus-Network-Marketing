using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Services;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";
        ViewData["q"] = q;

        var query = db.Products.Include(p => p.Vendor).Include(p => p.Stock).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Name.Contains(term) || (p.Description != null && p.Description.Contains(term)));
        }

        var result = await PagedResult<Product>.CreateAsync(query.OrderBy(p => p.Name), page);
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var product = await db.Products
            .Include(p => p.Vendor)
            .Include(p => p.Stock)
            .Include(p => p.EquipmentItems)
            .Include(p => p.OrderItems).ThenInclude(oi => oi.Order)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["PanelName"] = "Admin Panel";
        await LoadVendorsAsync();
        return View(new ProductFormViewModel { ReorderLevel = 5 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        if (!ModelState.IsValid)
        {
            await LoadVendorsAsync();
            return View(model);
        }

        var product = new Product
        {
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            UnitOfMeasure = model.UnitOfMeasure.Trim(),
            UnitCost = model.UnitCost,
            UnitSalePrice = model.UnitSalePrice,
            VendorId = model.VendorId,
            IsActive = model.IsActive,
            Stock = new Stock
            {
                QuantityOnHand = model.OpeningQuantity,
                QuantityReserved = 0,
                ReorderLevel = model.ReorderLevel
            }
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Product '{product.Name}' was created with opening stock.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var product = await db.Products.Include(p => p.Stock).AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        await LoadVendorsAsync();
        return View(new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            UnitOfMeasure = product.UnitOfMeasure,
            UnitCost = product.UnitCost,
            UnitSalePrice = product.UnitSalePrice,
            VendorId = product.VendorId,
            IsActive = product.IsActive,
            OpeningQuantity = product.Stock?.QuantityOnHand ?? 0,
            ReorderLevel = product.Stock?.ReorderLevel ?? 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        var product = await db.Products.Include(p => p.Stock).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadVendorsAsync();
            return View(model);
        }

        product.Name = model.Name.Trim();
        product.Description = model.Description?.Trim();
        product.UnitOfMeasure = model.UnitOfMeasure.Trim();
        product.UnitCost = model.UnitCost;
        product.UnitSalePrice = model.UnitSalePrice;
        product.VendorId = model.VendorId;
        product.IsActive = model.IsActive;

        if (product.Stock is not null)
        {
            product.Stock.ReorderLevel = model.ReorderLevel;
            product.Stock.LastUpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Product details were updated. Use the Stock module to change quantities.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var product = await db.Products.Include(p => p.Vendor).AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        var hasDependencies = await db.OrderItems.AnyAsync(oi => oi.ProductId == id) || await db.Equipment.AnyAsync(e => e.ProductId == id);
        if (hasDependencies)
        {
            product.IsActive = false;
            await db.SaveChangesAsync();
            TempData["Success"] = "This product is referenced by orders or equipment, so it was deactivated instead of deleted.";
            return RedirectToAction(nameof(Index));
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        TempData["Success"] = "Product was deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadVendorsAsync()
    {
        ViewBag.Vendors = new SelectList(
            await db.Vendors.AsNoTracking().Where(v => v.IsActive).OrderBy(v => v.Name).ToListAsync(),
            "Id", "Name");
    }
}
