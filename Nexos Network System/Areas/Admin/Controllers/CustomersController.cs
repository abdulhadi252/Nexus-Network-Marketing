using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

using CustomerModel = Nexos_Network_System.Models.Customer;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CustomersController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, bool? isActive, int? cityId, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";

        var query = db.Customers
            .Include(c => c.Addresses).ThenInclude(a => a.City)
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
                c.PrimaryContactNumber.Contains(term) ||
                (c.NationalId != null && c.NationalId.Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (cityId.HasValue)
        {
            query = query.Where(c => c.Addresses.Any(a => a.CityId == cityId.Value));
        }

        var customers = await PagedResult<CustomerModel>.CreateAsync(
            query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName), page);

        // The tiles summarise the whole register, so they are counted independently of the filters.
        var createdAt = db.Customers.Select(c => c.CreatedAt);
        var newCities = await db.CustomerAddresses.Select(a => a.CityId).Distinct().CountAsync();
        var connections = await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Active);
        var allOrders = db.Orders.AsNoTracking();
        var pendingOrders = await allOrders.CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending);

        var model = new CustomerIndexViewModel
        {
            Customers = customers,
            TotalCustomers = await db.Customers.CountAsync(),
            ActiveConnections = connections,
            ServiceCities = newCities,
            Query = q,
            IsActive = isActive,
            CityId = cityId,
            Cities = await db.Cities.AsNoTracking().OrderBy(c => c.Name).ToListAsync()
        };

        // Sparkline KPI tiles above the directory, Messages-page style.
        ViewData["TotalCustomers"] = await AdminKpi.CountAsync(createdAt, db, "vs. last 30 days");
        ViewData["ActiveConnections"] = new BillMetric { Value = connections, Foot = "Active Connections", ShowFootArrow = false };
        ViewData["PendingOrders"] = new BillMetric { Value = pendingOrders, Foot = "Orders Waiting", ShowFootArrow = false };
        ViewData["ServiceCities"] = new BillMetric { Value = newCities, Foot = "Service Cities", ShowFootArrow = false };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var customer = await db.Customers
            .Include(c => c.Addresses).ThenInclude(a => a.City)
            .Include(c => c.Orders).ThenInclude(o => o.Plan)
            .Include(c => c.Connections).ThenInclude(x => x.Plan)
            .Include(c => c.Bills)
            .Include(c => c.Payments)
            .Include(c => c.FeedbackEntries)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null)
        {
            return NotFound();
        }

        return View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["PanelName"] = "Admin Panel";
        await LoadCitiesAsync();
        return View(new CustomerFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        if (await db.Customers.AnyAsync(c => c.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "A customer with this email already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadCitiesAsync();
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

        TempData["Success"] = $"Customer '{customer.FirstName} {customer.LastName}' was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var customer = await db.Customers.Include(c => c.Addresses).AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (customer is null)
        {
            return NotFound();
        }

        var address = customer.Addresses.OrderByDescending(a => a.IsPrimary).FirstOrDefault();
        await LoadCitiesAsync();

        return View(new CustomerFormViewModel
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            NationalId = customer.NationalId,
            Email = customer.Email,
            PrimaryContactNumber = customer.PrimaryContactNumber,
            AlternateContactNumber = customer.AlternateContactNumber,
            AddressLine1 = address?.AddressLine1 ?? string.Empty,
            AddressLine2 = address?.AddressLine2,
            PostalCode = address?.PostalCode,
            CityId = address?.CityId ?? 0,
            IsActive = customer.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        var customer = await db.Customers.Include(c => c.Addresses).FirstOrDefaultAsync(c => c.Id == id);
        if (customer is null)
        {
            return NotFound();
        }

        if (await db.Customers.AnyAsync(c => c.Email == model.Email && c.Id != id))
        {
            ModelState.AddModelError(nameof(model.Email), "Another customer already uses this email.");
        }

        if (!ModelState.IsValid)
        {
            await LoadCitiesAsync();
            return View(model);
        }

        customer.FirstName = model.FirstName.Trim();
        customer.LastName = model.LastName.Trim();
        customer.NationalId = model.NationalId?.Trim();
        customer.Email = model.Email.Trim();
        customer.PrimaryContactNumber = model.PrimaryContactNumber.Trim();
        customer.AlternateContactNumber = model.AlternateContactNumber?.Trim();
        customer.IsActive = model.IsActive;

        var address = customer.Addresses.OrderByDescending(a => a.IsPrimary).FirstOrDefault();
        if (address is null)
        {
            address = new CustomerAddress { CustomerId = customer.Id, AddressLine1 = string.Empty, IsPrimary = true };
            customer.Addresses.Add(address);
        }

        address.CityId = model.CityId;
        address.AddressLine1 = model.AddressLine1.Trim();
        address.AddressLine2 = model.AddressLine2?.Trim();
        address.PostalCode = model.PostalCode?.Trim();

        await db.SaveChangesAsync();

        TempData["Success"] = "Customer details were updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (customer is null)
        {
            return NotFound();
        }

        return View(customer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer is null)
        {
            return NotFound();
        }

        customer.IsActive = false;
        await db.SaveChangesAsync();

        TempData["Success"] = "Customer was deactivated. Existing orders, connections, bills and payments are preserved.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCitiesAsync()
    {
        ViewBag.Cities = new SelectList(
            await db.Cities.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
            "Id", "Name");
    }
}
