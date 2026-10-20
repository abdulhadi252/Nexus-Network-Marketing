using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;

namespace Nexos_Network_System.Areas.Admin.Controllers;

/// <summary>
/// Read-and-review view of every feedback a customer submitted; the customer panel
/// only ever shows its own rows.
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class FeedbackController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? query, string? status, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";

        var allFeedback = db.FeedbackEntries.AsNoTracking();

        ViewData["TotalFeedback"] = await allFeedback.CountAsync();
        ViewData["PendingFeedback"] = await allFeedback.CountAsync(f => !f.IsReviewed);
        ViewData["ReviewedFeedback"] = await allFeedback.CountAsync(f => f.IsReviewed);

        var ratings = await allFeedback
            .Where(f => f.Rating != null)
            .Select(f => f.Rating!.Value)
            .ToListAsync();
        ViewData["AverageRating"] = ratings.Count == 0 ? 0d : ratings.Average(rating => (double)rating);
        ViewData["RatingCount"] = ratings.Count;

        IQueryable<Feedback> filtered = allFeedback
            .Include(f => f.Customer)
            .Include(f => f.Connection);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            filtered = filtered.Where(f =>
                f.Message.Contains(term) ||
                (f.Customer.FirstName + " " + f.Customer.LastName).Contains(term) ||
                f.Customer.Email.Contains(term));
        }

        filtered = status switch
        {
            "pending" => filtered.Where(f => !f.IsReviewed),
            "reviewed" => filtered.Where(f => f.IsReviewed),
            _ => filtered
        };

        var paged = await PagedResult<Feedback>.CreateAsync(
            filtered.OrderByDescending(f => f.SubmittedAt), page);

        ViewData["Query"] = query;
        ViewData["Status"] = status;

        return View(paged);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleReviewed(int id, string? query, string? status, int page = 1)
    {
        var feedback = await db.FeedbackEntries.FirstOrDefaultAsync(f => f.Id == id);
        if (feedback is null)
        {
            return NotFound();
        }

        feedback.IsReviewed = !feedback.IsReviewed;
        await db.SaveChangesAsync();

        TempData["Success"] = feedback.IsReviewed
            ? "Feedback marked as reviewed."
            : "Feedback returned to the pending review list.";

        return RedirectToAction(nameof(Index), new { query, status, page });
    }
}
