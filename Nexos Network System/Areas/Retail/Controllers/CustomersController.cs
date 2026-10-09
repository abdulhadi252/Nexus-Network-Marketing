using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;

using CustomerModel = Nexos_Network_System.Models.Customer;

namespace Nexos_Network_System.Areas.Retail.Controllers;

[Area("Retail")]
[Authorize(Roles = "Retail")]
public class CustomersController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        ViewData["PanelName"] = "Retail Panel";
        ViewData["q"] = q;

        var retailShopId = await User.GetRetailShopIdAsync(db);
        if (retailShopId is null)
        {
            return Forbid();
        }

        var shopCityId = await db.RetailShops.Where(s => s.Id == retailShopId.Value)
            .Select(s => s.CityId)
            .FirstAsync();
        var query = db.Customers
            .Include(c => c.Addresses).ThenInclude(a => a.City)
            .Include(c => c.Connections)
            .AsNoTracking()
            .Where(c => c.Addresses.Any(a => a.CityId == shopCityId))
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

        var result = await PagedResult<CustomerModel>.CreateAsync(query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName), page);
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

        var shopCityId = await db.RetailShops.Where(s => s.Id == retailShopId.Value)
            .Select(s => s.CityId)
            .FirstAsync();
        var customer = await db.Customers
            .Include(c => c.Addresses).ThenInclude(a => a.City)
            .Include(c => c.Orders.Where(o => o.RetailShopId == retailShopId.Value)).ThenInclude(o => o.Plan)
            .Include(c => c.Connections.Where(x => x.Order != null && x.Order.RetailShopId == retailShopId.Value)).ThenInclude(x => x.Plan)
            .Include(c => c.Bills.Where(b => b.Connection.Order != null && b.Connection.Order.RetailShopId == retailShopId.Value))
            .Include(c => c.Payments.Where(p => p.Connection.Order != null && p.Connection.Order.RetailShopId == retailShopId.Value))
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.Addresses.Any(a => a.CityId == shopCityId));

        if (customer is null)
        {
            return NotFound();
        }

        return View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["PanelName"] = "Retail Panel";
        var retailShopId = await User.GetRetailShopIdAsync(db);
        if (retailShopId is null)
        {
            return Forbid();
        }

        var shopCityId = await db.RetailShops.Where(s => s.Id == retailShopId.Value)
            .Select(s => s.CityId)
            .FirstAsync();
        await LoadCitiesAsync(shopCityId);
        return View(new CustomerFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model)
    {
        ViewData["PanelName"] = "Retail Panel";

        var retailShopId = await User.GetRetailShopIdAsync(db);
        if (retailShopId is null)
        {
            return Forbid();
        }

        var shopCityId = await db.RetailShops.Where(s => s.Id == retailShopId.Value)
            .Select(s => s.CityId)
            .FirstAsync();

        if (await db.Customers.AnyAsync(c => c.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "A customer with this email already exists.");
        }

        if (model.CityId != shopCityId)
        {
            ModelState.AddModelError(nameof(model.CityId), "Customers can only be registered in the assigned shop's service area.");
        }

        if (!ModelState.IsValid)
        {
            await LoadCitiesAsync(shopCityId);
            return View(model);
        }

        var customer = new CustomerModel
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            NationalId = model.NationalId?.Trim(),
            Email = model.Email.Trim(),
            PrimaryContactNumber = model.PrimaryContactNumber.Trim(),
            AlternateContactNumber = model.AlternateContactNumber?.Trim(),
            IsActive = model.IsActive
        };
        customer.Addresses.Add(new CustomerAddress
        {
            CityId = model.CityId,
            AddressLine1 = model.AddressLine1.Trim(),
            AddressLine2 = model.AddressLine2?.Trim(),
            PostalCode = model.PostalCode?.Trim(),
            IsPrimary = true
        });

        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Customer '{customer.FirstName} {customer.LastName}' was registered. You can now place an order.";
        return RedirectToAction(nameof(Details), new { id = customer.Id });
    }

    private async Task LoadCitiesAsync(int shopCityId)
    {
        ViewBag.Cities = new SelectList(
            await db.Cities.AsNoTracking().Where(c => c.Id == shopCityId).OrderBy(c => c.Name).ToListAsync(),
            "Id", "Name");
    }
}
