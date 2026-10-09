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

namespace Nexos_Network_System.Areas.Retail.Controllers;

[Area("Retail")]
[Authorize(Roles = "Retail")]
public class OrdersController(ApplicationDbContext db, IOrderService orderService) : Controller
{
    public async Task<IActionResult> Index(OrderFilterViewModel filter, int page = 1)
    {
        ViewData["PanelName"] = "Retail Panel";
        filter.Page = page;

        var retailShopId = await User.GetRetailShopIdAsync(db);
        if (retailShopId is null)
        {
            return Forbid();
        }

        var query = db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.Connection)
            .AsNoTracking()
            .Where(o => o.RetailShopId == retailShopId.Value)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var term = filter.Query.Trim();
            query = query.Where(o =>
                o.OrderNumber.Contains(term) ||
                (o.Customer.FirstName + " " + o.Customer.LastName).Contains(term));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        if (filter.ConnectionType.HasValue)
        {
            query = query.Where(o => o.ConnectionType == filter.ConnectionType.Value);
        }

        var result = await PagedResult<Order>.CreateAsync(query.OrderByDescending(o => o.SubmittedAt), page);
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Retail Panel";
        var retailShopId = await User.GetRetailShopIdAsync(db);
        if (retailShopId is null)
        {
            return Forbid();
        }

        var order = await db.Orders
            .Include(o => o.Customer).ThenInclude(c => c.Addresses).ThenInclude(a => a.City)
            .Include(o => o.RetailShop)
            .Include(o => o.Plan)
            .Include(o => o.ProcessedByEmployee)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.FeasibilityReport)
            .Include(o => o.Connection)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && o.RetailShopId == retailShopId.Value);

        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Track(string? orderNumber)
    {
        ViewData["PanelName"] = "Retail Panel";

        var retailShopId = await User.GetRetailShopIdAsync(db);
        if (retailShopId is null)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return View(null);
        }

        var order = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.Connection)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber.Trim().ToUpper() &&
                                      o.RetailShopId == retailShopId.Value);

        ViewData["SearchTerm"] = orderNumber;
        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId)
    {
        ViewData["PanelName"] = "Retail Panel";
        var retailShopId = await User.GetRetailShopIdAsync(db);
        if (retailShopId is null)
        {
            return Forbid();
        }

        await LoadLookupsAsync(retailShopId.Value);
        return View(new OrderCreateViewModel
        {
            CustomerId = customerId ?? 0,
            RetailShopId = retailShopId.Value
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderCreateViewModel model)
    {
        ViewData["PanelName"] = "Retail Panel";
        var retailShopId = await User.GetRetailShopIdAsync(db);
        if (retailShopId is null)
        {
            return Forbid();
        }

        if (model.Items.All(i => i.ProductId <= 0 || i.Quantity <= 0))
        {
            model.Items.Clear();
        }

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(retailShopId.Value);
            return View(model);
        }

        var employee = await User.GetEmployeeAsync(db);

        try
        {
            var orderId = await orderService.CreateOrderAsync(model, retailShopId, employee?.Id);
            var order = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
            TempData["Success"] = $"Order {order.OrderNumber} was created successfully.";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadLookupsAsync(retailShopId.Value);
            return View(model);
        }
    }

    private async Task LoadLookupsAsync(int retailShopId)
    {
        var shop = await db.RetailShops.AsNoTracking()
            .Where(s => s.Id == retailShopId && s.IsActive)
            .Select(s => new { s.CityId, s.Name })
            .FirstOrDefaultAsync();

        if (shop is null)
        {
            throw new InvalidOperationException("The assigned retail shop is no longer active.");
        }

        ViewBag.RetailShopName = shop.Name;
        ViewBag.Customers = new SelectList(
            await db.Customers.AsNoTracking().Where(c => c.IsActive)
                .Where(c => c.Addresses.Any(a => a.CityId == shop.CityId))
                .OrderBy(c => c.FirstName)
                .Select(c => new { c.Id, Name = c.FirstName + " " + c.LastName + " (" + c.PrimaryContactNumber + ")" })
                .ToListAsync(),
            "Id", "Name");

        ViewBag.Plans = new SelectList(
            await db.Plans.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.ConnectionType).ThenBy(p => p.Name)
                .Select(p => new { p.Id, Label = p.ConnectionType + " - " + p.Name + " (PKR " + p.RecurringCharge + ")" })
                .ToListAsync(),
            "Id", "Label");

        ViewBag.RetailShopId = retailShopId;

        ViewBag.Products = await db.Products
            .AsNoTracking()
            .Include(p => p.Stock)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, Available = p.Stock == null ? 0 : p.Stock.QuantityOnHand - p.Stock.QuantityReserved })
            .ToListAsync();
    }
}
