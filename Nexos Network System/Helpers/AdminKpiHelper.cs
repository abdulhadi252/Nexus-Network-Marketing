using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Helpers;

/// <summary>
/// Builds the figures behind the KPI tiles on the admin list pages.
///
/// Dated queries turn into a six-month monthly series — the same window the
/// dashboard and billing list sparklines draw from — while figure-only tiles
/// (stock levels, inactive counts) come out as value-only metrics through
/// <see cref="Static"/>. All tiles share the one BillMetric shape the views render.
/// </summary>
public static class AdminKpi
{
    /// <summary>A tile for a figure with no meaningful monthly history; no arrow is drawn.</summary>
    public static BillMetric Static(double value, string foot) => new()
    {
        Value = value,
        Foot = foot,
        ShowFootArrow = false
    };

    public static async Task<BillMetric> CountAsync(
        IQueryable<DateTimeOffset> moments,
        ApplicationDbContext db,
        string foot,
        CancellationToken cancellationToken = default)
    {
        var windowStart = MonthlySeries.StartOfMonth(DateTimeOffset.UtcNow.AddMonths(-5));
        var series = await MonthlySeries.CountsAsync(moments, windowStart, 6, cancellationToken);

        return new BillMetric
        {
            Value = series.Sum(),
            Trend = MonthlySeries.Tail(series, 5),
            Foot = foot,
            ShowFootArrow = true,
            FootRises = series[^1] >= (series.Count > 1 ? series[^2] : 0)
        };
    }

    public static async Task<BillMetric> SumAsync(
        IQueryable<(DateTimeOffset Moment, double Amount)> rows,
        ApplicationDbContext db,
        string foot,
        CancellationToken cancellationToken = default)
    {
        var windowStart = MonthlySeries.StartOfMonth(DateTimeOffset.UtcNow.AddMonths(-5));
        var series = MonthlySeries.Sums(await rows.ToListAsync(cancellationToken), windowStart, 6);

        return new BillMetric
        {
            Value = series.Sum(),
            Trend = MonthlySeries.Tail(series, 5),
            Foot = foot,
            ShowFootArrow = true,
            FootRises = series[^1] >= (series.Count > 1 ? series[^2] : 0)
        };
    }
}
