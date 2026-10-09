using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class FeedbackController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record.";
            return View(new List<Feedback>());
        }

        var feedback = await db.FeedbackEntries
            .Include(f => f.Connection)
            .AsNoTracking()
            .Where(f => f.CustomerId == customerId.Value)
            .OrderByDescending(f => f.SubmittedAt)
            .ToListAsync();

        return View(feedback);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["PanelName"] = "Customer Panel";
        await LoadConnectionsAsync();
        return View(new FeedbackViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeedbackViewModel model)
    {
        ViewData["PanelName"] = "Customer Panel";

        var customerId = await User.GetCustomerIdAsync(db);
        if (customerId is null)
        {
            TempData["Error"] = "Your login is not linked to a customer record.";
            return RedirectToAction(nameof(Index));
        }

        if (model.ConnectionId.HasValue)
        {
            var ownsConnection = await db.Connections.AnyAsync(c => c.Id == model.ConnectionId.Value && c.CustomerId == customerId.Value);
            if (!ownsConnection)
            {
                ModelState.AddModelError(nameof(model.ConnectionId), "Select one of your own connections.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadConnectionsAsync();
            return View(model);
        }

        db.FeedbackEntries.Add(new Feedback
        {
            CustomerId = customerId.Value,
            ConnectionId = model.ConnectionId,
            Rating = model.Rating,
            Message = model.Message.Trim(),
            SubmittedAt = DateTimeOffset.UtcNow,
            IsReviewed = false
        });
        await db.SaveChangesAsync();

        TempData["Success"] = "Thank you for your feedback. Our team reviews every submission.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadConnectionsAsync()
    {
        var customerId = await User.GetCustomerIdAsync(db);
        var connections = await db.Connections
            .Where(c => c.CustomerId == customerId)
            .OrderBy(c => c.AccountNumber)
            .Select(c => new { c.Id, Label = c.AccountNumber + " (" + c.ConnectionType + ")" })
            .ToListAsync();

        ViewBag.Connections = new SelectList(connections, "Id", "Label");
    }
}
