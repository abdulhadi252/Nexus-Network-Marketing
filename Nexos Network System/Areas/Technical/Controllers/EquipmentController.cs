using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Technical.Controllers;

[Area("Technical")]
[Authorize(Roles = "Technical")]
public class EquipmentController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, EquipmentStatus? status, int page = 1)
    {
        ViewData["PanelName"] = "Technical Panel";
        ViewData["q"] = q;
        ViewData["status"] = status;

        var query = db.Equipment
            .Include(e => e.Product)
            .Include(e => e.Connection).ThenInclude(c => c!.Customer)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(e => e.AssetTag.Contains(term) || (e.SerialNumber != null && e.SerialNumber.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        var result = await PagedResult<Equipment>.CreateAsync(query.OrderBy(e => e.AssetTag), page);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Assign(int? equipmentId)
    {
        ViewData["PanelName"] = "Technical Panel";
        await LoadLookupsAsync();
        return View(new EquipmentAssignViewModel { EquipmentId = equipmentId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(EquipmentAssignViewModel model)
    {
        ViewData["PanelName"] = "Technical Panel";

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var equipment = await db.Equipment.FirstOrDefaultAsync(e => e.Id == model.EquipmentId);
        if (equipment is null)
        {
            ModelState.AddModelError(string.Empty, "Selected equipment was not found.");
        }
        else if (equipment.Status != EquipmentStatus.Available)
        {
            ModelState.AddModelError(string.Empty, "Only available equipment can be assigned.");
        }

        var connection = await db.Connections.FirstOrDefaultAsync(c =>
            c.Id == model.ConnectionId &&
            (c.Status == ConnectionStatus.Active || c.Status == ConnectionStatus.Pending));
        if (connection is null)
        {
            ModelState.AddModelError(string.Empty, "Select an active or pending connection.");
        }

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(model);
        }

        equipment!.Status = EquipmentStatus.Assigned;
        equipment.ConnectionId = connection!.Id;
        equipment.AssignedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] = $"Equipment {equipment.AssetTag} was assigned to {connection.AccountNumber}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id)
    {
        var equipment = await db.Equipment.FirstOrDefaultAsync(e => e.Id == id);
        if (equipment is null)
        {
            return NotFound();
        }

        if (equipment.Status != EquipmentStatus.Assigned || equipment.ConnectionId is null)
        {
            TempData["Error"] = "Only assigned equipment can be returned.";
            return RedirectToAction(nameof(Index));
        }

        equipment.Status = EquipmentStatus.Available;
        equipment.ConnectionId = null;
        equipment.AssignedAt = null;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Equipment {equipment.AssetTag} was returned to inventory.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRepair(int id)
    {
        var equipment = await db.Equipment.FirstOrDefaultAsync(e => e.Id == id);
        if (equipment is null)
        {
            return NotFound();
        }

        if (equipment.Status is not (EquipmentStatus.Available or EquipmentStatus.Assigned))
        {
            TempData["Error"] = "Only available or assigned equipment can be marked for repair.";
            return RedirectToAction(nameof(Index));
        }

        equipment.Status = EquipmentStatus.UnderRepair;
        equipment.ConnectionId = null;
        equipment.AssignedAt = null;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Equipment {equipment.AssetTag} was marked as under repair.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteRepair(int id)
    {
        var equipment = await db.Equipment.FirstOrDefaultAsync(e => e.Id == id);
        if (equipment is null)
        {
            return NotFound();
        }

        if (equipment.Status != EquipmentStatus.UnderRepair)
        {
            TempData["Error"] = "Only equipment under repair can be returned to available stock.";
            return RedirectToAction(nameof(Index));
        }

        equipment.Status = EquipmentStatus.Available;
        equipment.ConnectionId = null;
        equipment.AssignedAt = null;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Equipment {equipment.AssetTag} is available for assignment again.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Customer-raised cancellation and return requests. Defaults to the pending queue, which is
    /// what technical has to act on; the status filter opens the reviewed history instead.
    /// </summary>
    public async Task<IActionResult> Requests(EquipmentRequestStatus? status = EquipmentRequestStatus.Pending)
    {
        ViewData["PanelName"] = "Technical Panel";
        ViewData["status"] = status;

        var query = db.EquipmentRequests
            .Include(r => r.Equipment).ThenInclude(e => e.Product)
            .Include(r => r.Customer)
            .Include(r => r.Equipment).ThenInclude(e => e.Connection)
            .Include(r => r.ReviewedByEmployee)
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var requests = await query
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

        ViewBag.PendingCount = await db.EquipmentRequests.CountAsync(r => r.Status == EquipmentRequestStatus.Pending);

        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewRequest(int id, EquipmentRequestStatus decision, string? notes)
    {
        var request = await db.EquipmentRequests
            .Include(r => r.Equipment)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != EquipmentRequestStatus.Pending)
        {
            TempData["Error"] = "This request has already been reviewed.";
            return RedirectToAction(nameof(Requests));
        }

        if (decision is not (EquipmentRequestStatus.Approved or EquipmentRequestStatus.Rejected))
        {
            TempData["Error"] = "Choose whether to approve or reject the request.";
            return RedirectToAction(nameof(Requests));
        }

        var employee = await User.GetEmployeeAsync(db);

        request.Status = decision;
        request.ReviewedAt = DateTimeOffset.UtcNow;
        request.ReviewedByEmployeeId = employee?.Id;
        request.ReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        if (decision == EquipmentRequestStatus.Approved && request.Equipment is not null)
        {
            // Approving takes the unit back into inventory so it can be assigned again.
            request.Equipment.Status = EquipmentStatus.Available;
            request.Equipment.ConnectionId = null;
            request.Equipment.AssignedAt = null;
        }

        await db.SaveChangesAsync();

        TempData["Success"] = decision == EquipmentRequestStatus.Approved
            ? $"{request.RequestType} request for {request.Equipment?.AssetTag} was approved and the equipment is back in inventory."
            : $"{request.RequestType} request for {request.Equipment?.AssetTag} was rejected.";

        return RedirectToAction(nameof(Requests));
    }

    private async Task LoadLookupsAsync()
    {
        var available = await db.Equipment
            .Where(e => e.Status == EquipmentStatus.Available)
            .Include(e => e.Product)
            .OrderBy(e => e.AssetTag)
            .Select(e => new { e.Id, Label = e.AssetTag + " - " + e.Product.Name })
            .ToListAsync();
        ViewBag.AvailableEquipment = new SelectList(available, "Id", "Label");

        var connections = await db.Connections
            .Where(c => c.Status == ConnectionStatus.Active || c.Status == ConnectionStatus.Pending)
            .OrderBy(c => c.AccountNumber)
            .Select(c => new { c.Id, Label = c.AccountNumber + " - " + c.Customer.FirstName + " " + c.Customer.LastName })
            .ToListAsync();
        ViewBag.Connections = new SelectList(connections, "Id", "Label");
    }
}
