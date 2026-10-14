using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

/// <summary>
/// Backs the admin billing list: the paged bills plus the four summary tiles shown above them.
/// </summary>
public class BillIndexViewModel
{
    public PagedResult<Bill> Bills { get; init; } = new();

    /// <summary>Every bill on record; the sparkline is the monthly running total.</summary>
    public BillMetric TotalBills { get; init; } = new();

    /// <summary>Amount still due on the latest bill of each connection.</summary>
    public BillMetric Outstanding { get; init; } = new();

    /// <summary>Service tax rate applied to taxable billing.</summary>
    public BillMetric ServiceTax { get; init; } = new();

    public BillMetric ThisMonth { get; init; } = new();

    public string? Query { get; init; }

    public BillStatus? Status { get; init; }

    /// <summary>1-based number of the first row on the current page, or 0 when the list is empty.</summary>
    public int FirstRowNumber => Bills.Items.Count == 0
        ? 0
        : ((Bills.Page - 1) * Bills.PageSize) + 1;

    /// <summary>1-based number of the last row on the current page.</summary>
    public int LastRowNumber => ((Bills.Page - 1) * Bills.PageSize) + Bills.Items.Count;
}

/// <summary>
/// One summary tile: the headline figure, the monthly series drawn behind it and the line beneath it.
/// </summary>
public class BillMetric
{
    public double Value { get; set; }

    /// <summary>Monthly series, oldest first. Only drawn when there is more than one point.</summary>
    public IReadOnlyList<double> Trend { get; set; } = Array.Empty<double>();

    public bool HasTrend => Trend.Count > 1;

    /// <summary>Display text for the foot line, for example "2 this week" or "of total bill amount".</summary>
    public string Foot { get; set; } = string.Empty;

    /// <summary>False when the foot line is a plain caption marked with an info icon instead of an arrow.</summary>
    public bool ShowFootArrow { get; set; }

    /// <summary>Arrow direction; "down" is also how a fall in the figure is shown.</summary>
    public bool FootRises { get; set; }

    /// <summary>True when the movement shown by the arrow is the unwanted outcome, which colours it red.</summary>
    public bool FootIsBad { get; set; }
}
