using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.Services;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class StockController(ApplicationDbContext db, IStockService stockService) : Controller
{
    public async Task<IActionResult> Index(string? q, bool lowOnly = false, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";
        ViewData["q"] = q;
        ViewData["lowOnly"] = lowOnly;

        var query = db.StockItems.Include(s => s.Product).ThenInclude(p => p.Vendor).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s => s.Product.Name.Contains(term));
        }

        if (lowOnly)
        {
            query = query.Where(s => s.QuantityOnHand <= s.ReorderLevel);
        }

        var result = await PagedResult<Stock>.CreateAsync(query.OrderBy(s => s.Product.Name), page);

        // Sparkline KPI tiles above the list, Messages-page style.
        var allStock = await db.StockItems.AsNoTracking().ToListAsync();

        ViewData["kpiSkus"] = AdminKpi.Static(allStock.Count, "stocked products");
        ViewData["kpiLow"] = AdminKpi.Static(allStock.Count(s => s.QuantityOnHand <= s.ReorderLevel), "low stock items");
        ViewData["kpiOnHand"] = AdminKpi.Static(allStock.Sum(s => s.QuantityOnHand), "units on hand");
        ViewData["kpiReserved"] = AdminKpi.Static(allStock.Sum(s => s.QuantityReserved), "units reserved");

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Adjust(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var stock = await db.StockItems.Include(s => s.Product).AsNoTracking().FirstOrDefaultAsync(s => s.ProductId == id);
        if (stock is null)
        {
            return NotFound();
        }

        return View(new StockAdjustViewModel
        {
            ProductId = stock.ProductId,
            ProductName = stock.Product.Name,
            QuantityOnHand = stock.QuantityOnHand,
            QuantityReserved = stock.QuantityReserved,
            ReorderLevel = stock.ReorderLevel
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(int id, StockAdjustViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        if (!ModelState.IsValid)
        {
            var existing = await db.StockItems.Include(s => s.Product).AsNoTracking().FirstOrDefaultAsync(s => s.ProductId == id);
            if (existing is null)
            {
                return NotFound();
            }

            model.ProductName = existing.Product.Name;
            model.QuantityOnHand = existing.QuantityOnHand;
            model.QuantityReserved = existing.QuantityReserved;
            model.ReorderLevel = existing.ReorderLevel;
            return View(model);
        }

        var employee = await User.GetEmployeeAsync(db);

        try
        {
            await stockService.RecordMovementAsync(id, model.MovementType, model.Quantity, model.Reason, employee?.Id);
            TempData["Success"] = $"Stock for '{model.ProductName}' was updated.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Movements), new { id });
    }

    public async Task<IActionResult> Movements(int id, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";

        var stock = await db.StockItems.Include(s => s.Product).AsNoTracking().FirstOrDefaultAsync(s => s.ProductId == id);
        if (stock is null)
        {
            return NotFound();
        }

        ViewData["Product"] = stock.Product;

        var query = db.StockMovements
            .Include(m => m.PerformedByEmployee)
            .Where(m => m.StockId == stock.Id)
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt);

        var result = await PagedResult<StockMovement>.CreateAsync(query, page, 15);
        return View(result);
    }
}
