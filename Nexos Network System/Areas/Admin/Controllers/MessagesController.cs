using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;
using Nexos_Network_System.Areas.Admin.ViewModels;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MessagesController(
    ApplicationDbContext db,
    IEmailSender emailSender,
    ILogger<MessagesController> logger) : Controller
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

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var message = await db.ContactMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (message is null)
        {
            return NotFound();
        }

        return View(message);
    }

    [HttpGet]
    public async Task<IActionResult> Reply(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var message = await db.ContactMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (message is null)
        {
            return NotFound();
        }

        return View(new ReplyMessageViewModel
        {
            MessageId = message.Id,
            SenderName = message.Name,
            SenderEmail = message.Email,
            OriginalSubject = message.Subject,
            OriginalMessage = message.Message,
            SubmittedAt = message.SubmittedAt,
            Subject = ReplySubject(message.Subject)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, ReplyMessageViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (message is null)
        {
            return NotFound();
        }

        // The sender details come from the stored message, never from the posted form.
        model.MessageId = message.Id;
        model.SenderName = message.Name;
        model.SenderEmail = message.Email;
        model.OriginalSubject = message.Subject;
        model.OriginalMessage = message.Message;
        model.SubmittedAt = message.SubmittedAt;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await emailSender.SendAsync(new OutboundEmail(
                message.Email,
                message.Name,
                model.Subject.Trim(),
                model.Body.Trim()));
        }
        catch (EmailSendException ex)
        {
            logger.LogError(ex, "Replying to contact message {MessageId} failed", message.Id);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        message.Status = ContactMessageStatus.Replied;
        message.IsRead = true;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Reply sent to {message.Email}.";
        return RedirectToAction(nameof(Details), new { id = message.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRead(
        int id,
        bool unreadOnly = false,
        int page = 1,
        bool returnToDetails = false)
    {
        var message = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (message is null)
        {
            return NotFound();
        }

        message.IsRead = !message.IsRead;
        await db.SaveChangesAsync();

        TempData["Success"] = message.IsRead ? "Message marked as read." : "Message marked as unread.";

        // Called from the details page? Stay there instead of jumping back to the list.
        if (returnToDetails)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        return RedirectToAction(nameof(Index), new { unreadOnly, page });
    }

    /// <summary>
    /// Prefixes the reply subject with "Re: " unless it is already a reply.
    /// </summary>
    private static string ReplySubject(string subject)
    {
        var trimmed = subject.Trim();

        return trimmed.StartsWith("Re:", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"Re: {trimmed}";
    }
}
