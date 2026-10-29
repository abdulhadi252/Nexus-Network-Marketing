using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class EquipmentController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record.";
            return View(new List<Equipment>());
        }

        var equipment = await db.Equipment
            .Include(e => e.Product)
            .Include(e => e.Connection)
            .AsNoTracking()
            .Where(e => e.Connection != null && e.Connection.CustomerId == customerId.Value)
            .OrderBy(e => e.AssetTag)
            .ToListAsync();

        return View(equipment);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Customer Panel";

        var equipment = await LoadOwnedEquipmentAsync(id);
        if (equipment is null)
        {
            return NotFound();
        }

        var requests = await db.EquipmentRequests
            .AsNoTracking()
            .Where(r => r.EquipmentId == equipment.Id)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

        return View(new CustomerEquipmentDetailsViewModel
        {
            Equipment = equipment,
            Requests = requests
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RequestCancellation(int id, string? reason) =>
        CreateRequestAsync(id, EquipmentRequestType.Cancellation, reason);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RequestReturn(int id, string? reason) =>
        CreateRequestAsync(id, EquipmentRequestType.Return, reason);

    /// <summary>
    /// Both cancellation and return are requests: technical approves or rejects them from the
    /// technical panel, so the assignment is left untouched until then.
    /// </summary>
    private async Task<IActionResult> CreateRequestAsync(int id, EquipmentRequestType requestType, string? reason)
    {
        ViewData["PanelName"] = "Customer Panel";

        var equipment = await LoadOwnedEquipmentAsync(id);
        if (equipment is null)
        {
            return NotFound();
        }

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            return Forbid();
        }

        // Only the customer the unit is assigned to right now can raise a new request.
        if (equipment.Status != EquipmentStatus.Assigned || equipment.Connection?.CustomerId != customerId.Value)
        {
            TempData["Error"] = $"Equipment {equipment.AssetTag} is no longer assigned to your connection.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var hasOpenRequest = await db.EquipmentRequests
            .AnyAsync(r => r.EquipmentId == id && r.Status == EquipmentRequestStatus.Pending);
        if (hasOpenRequest)
        {
            TempData["Error"] = $"A request for equipment {equipment.AssetTag} is already waiting for technical review.";
            return RedirectToAction(nameof(Details), new { id });
        }

        db.EquipmentRequests.Add(new EquipmentRequest
        {
            EquipmentId = equipment.Id,
            CustomerId = customerId.Value,
            RequestType = requestType,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
        });
        await db.SaveChangesAsync();

        TempData["Success"] = requestType == EquipmentRequestType.Cancellation
            ? $"Cancellation request for {equipment.AssetTag} was sent to technical."
            : $"Return request for {equipment.AssetTag} was sent to technical.";

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Equipment the signed-in customer may see: units currently on their connections, plus units
    /// they already asked to cancel or return. The second case keeps the approved request and its
    /// technical note visible after the unit has gone back to inventory.
    /// </summary>
    private async Task<Equipment?> LoadOwnedEquipmentAsync(int id)
    {
        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            return null;
        }

        return await db.Equipment
            .Include(e => e.Product)
            .Include(e => e.Connection)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id &&
                ((e.Connection != null && e.Connection.CustomerId == customerId.Value) ||
                 db.EquipmentRequests.Any(r => r.EquipmentId == e.Id && r.CustomerId == customerId.Value)));
    }
}
