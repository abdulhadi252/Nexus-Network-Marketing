using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;
using Nexos_Network_System.Areas.Admin.ViewModels;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MessagesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(
        string? query,
        bool? unread,
        bool? replied,
        bool? pending,

        int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";

        var now = DateTimeOffset.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var totalMessages =
            await db.ContactMessages.AsNoTracking().CountAsync();
        var unreadCount =
            await db.ContactMessages.AsNoTracking().CountAsync(m => !m.IsRead);
        var repliedCount =
            await db.ContactMessages.AsNoTracking().CountAsync(m => m.Status == ContactMessageStatus.Replied);
        var pendingCount =
            await db.ContactMessages.AsNoTracking().CountAsync(m => m.Status == ContactMessageStatus.Pending
                && !m.IsRead);

        var totalSeries = await MonthlySeries.CountsAsync(
            db.ContactMessages.AsNoTracking().Select(m => m.SubmittedAt),
            MonthlySeries.StartOfMonth(now.AddMonths(-5)),
            6);
        var totalTrend = MonthlySeries.Tail(totalSeries, 5);
        var totalMetric = new BillMetric
        {
            Value = totalMessages,
            Trend = totalTrend,
            Foot = "vs. last 30 days",
            ShowFootArrow = true,
            FootRises = totalMessages >= totalSeries.LastOrDefault()
        };

        var unreadSeries = await MonthlySeries.CountsAsync(
            db.ContactMessages.AsNoTracking().Where(m => !m.IsRead).Select(m => m.SubmittedAt),
            MonthlySeries.StartOfMonth(now.AddMonths(-5)),
            6);
        var unreadTrend = MonthlySeries.Tail(unreadSeries, 5);
        var unreadMetric = new BillMetric
        {
            Value = unreadCount,
            Trend = unreadTrend,
            Foot = "vs. last 30 days",
            ShowFootArrow = true,
            FootRises = unreadCount >= unreadSeries.LastOrDefault()
        };

        var repliedSeries = await MonthlySeries.CountsAsync(
            db.ContactMessages.AsNoTracking().Where(m => m.Status == ContactMessageStatus.Replied).Select(m => m.SubmittedAt),
            MonthlySeries.StartOfMonth(now.AddMonths(-5)),
            6);
        var repliedTrend = MonthlySeries.Tail(repliedSeries, 5);
        var repliedMetric = new BillMetric
        {
            Value = repliedCount,
            Trend = repliedTrend,
            Foot = "vs. last 30 days",
            ShowFootArrow = true,
            FootRises = repliedCount >= repliedSeries.LastOrDefault()
        };

        var pendingSeries = await MonthlySeries.CountsAsync(
            db.ContactMessages.AsNoTracking().Where(m => m.Status == ContactMessageStatus.Pending && !m.IsRead).Select(m => m.SubmittedAt),
            MonthlySeries.StartOfMonth(now.AddMonths(-5)),
            6);
        var pendingTrend = MonthlySeries.Tail(pendingSeries, 5);
        var pendingMetric = new BillMetric
        {
            Value = pendingCount,
            Trend = pendingTrend,
            Foot = "vs. last 30 days",
            ShowFootArrow = true,
            FootIsBad = pendingCount < pendingSeries.LastOrDefault()
        };

        var baseQuery = db.ContactMessages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            query = query.Trim();
            baseQuery = baseQuery.Where(m =>
                m.Name.Contains(query) ||
                m.Email.Contains(query) ||
                m.Subject.Contains(query) ||
                m.Message.Contains(query));
        }

        if (unread == true)
        {
            baseQuery = baseQuery.Where(m => !m.IsRead);
        }
        else if (replied == true)
        {
            baseQuery = baseQuery.Where(m => m.Status == ContactMessageStatus.Replied);
        }
        else if (pending == true)
        {
            baseQuery = baseQuery.Where(m => m.Status == ContactMessageStatus.Pending && !m.IsRead);
        }

        var result = await PagedResult<ContactMessage>.CreateAsync(
            baseQuery.OrderByDescending(m => m.SubmittedAt), page);

        var vm = new MessageIndexViewModel(result)
        {
            Query = query,
            TotalMetric = totalMetric,
            UnreadMetric = unreadMetric,
            RepliedMetric = repliedMetric,
            PendingMetric = pendingMetric,
            SelectedTab = unread == true ? "Unread" : replied == true ? "Replied" : pending == true ? "Pending" : "All"
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRead(int id, bool unreadOnly = false, int page = 1)
    {
        var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (message is null)
        {
            return NotFound();
        }

        message.IsRead = !message.IsRead;
        await db.SaveChangesAsync();

        TempData["Success"] = message.IsRead ? "Message marked as read." : "Message marked as unread.";
        return RedirectToAction(nameof(Index), new { unreadOnly, page });
    }
}
