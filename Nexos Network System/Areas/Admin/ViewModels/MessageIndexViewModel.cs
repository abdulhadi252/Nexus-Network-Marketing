using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Admin.ViewModels;

/// <summary>
/// Backs the admin Messages list: the paged rows, the four summary tiles,
/// the current filter selections and the active tab pill.
/// </summary>
public class MessageIndexViewModel
{
    public PagedResult<ContactMessage> Messages { get; init; }

    /// <summary>
    /// Every message on record, regardless of the active filters.
    /// </summary>
    public int TotalMessages => Messages.TotalItems;

    /// <summary>
    /// Messages that have not been opened yet.
    /// </summary>
    public int UnreadCount { get; set; }

    /// <summary>
    /// Messages that have been replied to.
    /// </summary>
    public int RepliedCount { get; set; }

    /// <summary>
    /// Messages marked as pending that have not yet been read.
    /// </summary>
    public int PendingCount { get; set; }

    public string? Query { get; set; }

    /// <summary>
    /// Currently selected message status filter.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Date range filter value (today / week / month).
    /// </summary>
    public string? DateRange { get; set; }

    /// <summary>
    /// Current sort order: newest (default), oldest, name or subject.
    /// </summary>
    public string? Sort { get; set; }

    /// <summary>
    /// Advanced filter: only messages whose subject contains this text.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Advanced filter: only messages whose sender email contains this text.
    /// </summary>
    public string? FromEmail { get; set; }

    /// <summary>
    /// Advanced filter: only messages received on or after this date.
    /// </summary>
    public DateTimeOffset? FromDate { get; set; }

    /// <summary>
    /// Advanced filter: only messages received on or before this date.
    /// </summary>
    public DateTimeOffset? ToDate { get; set; }

    /// <summary>
    /// KPI tiles shown above the list.
    /// </summary>
    public BillMetric TotalMetric { get; set; } = new();

    public BillMetric UnreadMetric { get; set; } = new();

    public BillMetric RepliedMetric { get; set; } = new();

    public BillMetric PendingMetric { get; set; } = new();

    /// <summary>
    /// The active filter pill: All, Unread, Replied or Pending.
    /// </summary>
    public string SelectedTab { get; set; } = "All";

    public MessageIndexViewModel(
        PagedResult<ContactMessage> messages,
        int unreadCount = 0,
        int repliedCount = 0,
        int pendingCount = 0,
        BillMetric? totalMetric = null,
        BillMetric? unreadMetric = null,
        BillMetric? repliedMetric = null,
        BillMetric? pendingMetric = null)
    {
        Messages = messages;
        UnreadCount = unreadCount;
        RepliedCount = repliedCount;
        PendingCount = pendingCount;
        TotalMetric = totalMetric ?? new();
        UnreadMetric = unreadMetric ?? new();
        RepliedMetric = repliedMetric ?? new();
        PendingMetric = pendingMetric ?? new();
    }

    /// <summary>
    /// 1-based number of the first row on the current page, or 0 when the list is empty.
    /// </summary>
    public int FirstRowNumber => Messages.Items.Count == 0
        ? 0
        : ((Messages.Page - 1) * Messages.PageSize) + 1;

    /// <summary>
    /// 1-based number of the last row on the current page.
    /// </summary>
    public int LastRowNumber => ((Messages.Page - 1) * Messages.PageSize) + Messages.Items.Count;

    /// <summary>
    /// Shows the "Advanced Filters" button as expanded.
    /// </summary>
    public bool AdvancedFiltersExpanded { get; set; }
}
