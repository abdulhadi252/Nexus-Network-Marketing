using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;
using System.Diagnostics;

namespace Nexos_Network_System.Controllers;

public class HomeController(ApplicationDbContext db, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var plans = await db.Plans
            .AsNoTracking()
            .Where(p => p.IsActive && p.ConnectionType == ConnectionType.Broadband)
            .OrderBy(p => p.RecurringCharge)
            .Take(3)
            .ToListAsync();

        return View(plans);
    }

    public IActionResult About() => View();

    public IActionResult Services() => View();

    public IActionResult Privacy() => View();

    public IActionResult Terms() => View();

    public async Task<IActionResult> Plans()
    {
        var plans = await db.Plans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.ConnectionType)
            .ThenBy(p => p.RecurringCharge)
            .ToListAsync();

        return View(plans);
    }

    [HttpGet]
    public IActionResult Contact() => View(new ContactViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        db.ContactMessages.Add(new ContactMessage
        {
            Name = model.Name.Trim(),
            Email = model.Email.Trim(),
            Subject = model.Subject.Trim(),
            Message = model.Message.Trim()
        });
        await db.SaveChangesAsync();

        // The message is stored in the database and reviewed by the admin team from the
        // Admin > Messages inbox. No email is sent from the system at this stage.
        TempData["Success"] = "Your message has been saved and our team has been notified through the internal inbox. We will get back to you soon.";
        return RedirectToAction(nameof(Contact));
    }

    [HttpGet]
    public IActionResult TrackOrder() => View(new TrackOrderViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrackOrder(TrackOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var orderNumber = model.OrderNumber.Trim().ToUpperInvariant();
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Plan)
            .Include(o => o.Customer).ThenInclude(c => c.Addresses).ThenInclude(a => a.City)
            .Include(o => o.FeasibilityReport)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        model.Searched = true;
        if (order is not null)
        {
            model.Found = true;
            model.FoundOrderNumber = order.OrderNumber;
            model.ConnectionType = order.ConnectionType;
            model.Status = order.Status;
            model.SubmittedAt = order.SubmittedAt;
            model.PlanName = order.Plan?.Name;
            model.HasFeasibility = order.FeasibilityReport is not null;
            model.City = order.Customer.Addresses.OrderByDescending(a => a.IsPrimary).FirstOrDefault()?.City?.Name;
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult ConnectionStatus() => View(new ConnectionStatusViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConnectionStatus(ConnectionStatusViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var accountNumber = model.AccountNumber.Trim().ToUpperInvariant();
        var connection = await db.Connections
            .AsNoTracking()
            .Include(c => c.Plan)
            .Include(c => c.City)
            .FirstOrDefaultAsync(c => c.AccountNumber == accountNumber);

        model.Searched = true;
        if (connection is not null)
        {
            model.Found = true;
            model.FoundAccountNumber = connection.AccountNumber;
            model.ConnectionType = connection.ConnectionType;
            model.Status = connection.Status;
            model.PlanName = connection.Plan?.Name;
            model.ActivatedAt = connection.ActivatedAt;
            model.City = connection.City?.Name;
        }

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public new IActionResult StatusCode(int code)
    {
        ViewData["StatusCode"] = code;
        return View("StatusError", code);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        logger.LogError("Unhandled error. Request id: {RequestId}", Activity.Current?.Id ?? HttpContext.TraceIdentifier);
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
