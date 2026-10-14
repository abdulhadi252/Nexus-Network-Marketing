using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    /// <summary>Months shown in the sparkline of each KPI card.</summary>
    private const int TrendMonths = 6;

    /// <summary>Months shown on the connection growth chart.</summary>
    private const int ChartMonths = 12;

    private const double DonutRadius = 60;

    /// <summary>Legend order for the service distribution donut.</summary>
    private static readonly ConnectionType[] DistributionOrder =
        [ConnectionType.Broadband, ConnectionType.DialUp, ConnectionType.Telephone];

    public async Task<IActionResult> Index()
    {
        ViewData["PanelName"] = "Admin Panel";

        // The headline numbers describe the current calendar month; the charts look back
        // over a full year so seasonal movement stays visible.
        var monthStart = MonthlySeries.StartOfMonth(DateTimeOffset.UtcNow);
        var previousMonthStart = monthStart.AddMonths(-1);
        var windowStart = monthStart.AddMonths(-(ChartMonths - 1));

        var totalCustomers = await db.Customers.CountAsync();
        var activeConnections = await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Active);
        var pendingOrders = await db.Orders.CountAsync(o =>
            o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending);

        // ------------------------------------------------------------------
        // Series used by the charts: only the columns we bucket on are pulled
        // back, and only for the visible window.
        // ------------------------------------------------------------------
        var customerDates = await db.Customers
            .Where(c => c.CreatedAt >= windowStart)
            .Select(c => (DateTimeOffset?)c.CreatedAt)
            .ToListAsync();

        var connectionDates = await db.Connections
            .Where(c => (c.ActivatedAt ?? c.InstalledAt) >= windowStart)
            .Select(c => c.ActivatedAt ?? c.InstalledAt)
            .ToListAsync();

        var orderDates = await db.Orders
            .Where(o => o.SubmittedAt >= windowStart)
            .Select(o => (DateTimeOffset?)o.SubmittedAt)
            .ToListAsync();

        var paymentRows = await db.Payments
            .Where(p => p.Status == PaymentStatus.Recorded && p.PaidAt >= windowStart)
            .Select(p => new { p.PaidAt, p.Amount })
            .ToListAsync();

        var monthlyCustomers = MonthlySeries.Counts(customerDates, windowStart, ChartMonths);
        var monthlyConnections = MonthlySeries.Counts(connectionDates, windowStart, ChartMonths);
        var monthlyOrders = MonthlySeries.Counts(orderDates, windowStart, ChartMonths);
        var monthlyRevenue = MonthlySeries.Sums(
            paymentRows.Select(row => (row.PaidAt, (double)row.Amount)),
            windowStart,
            ChartMonths);

        // ------------------------------------------------------------------
        // Previous-month baselines, measured the same way as the headline value.
        // ------------------------------------------------------------------
        var customersBeforeThisMonth = await db.Customers.CountAsync(c => c.CreatedAt < monthStart);
        var activeConnectionsBeforeThisMonth = await db.Connections.CountAsync(c =>
            c.Status == ConnectionStatus.Active && (c.ActivatedAt ?? c.InstalledAt) < monthStart);
        var pendingOrdersBeforeThisMonth = await db.Orders.CountAsync(o =>
            (o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending) &&
            o.SubmittedAt < monthStart);

        var revenueThisMonth = await db.Payments
            .Where(p => p.Status == PaymentStatus.Recorded && p.PaidAt >= monthStart)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
        var revenueLastMonth = await db.Payments
            .Where(p => p.Status == PaymentStatus.Recorded && p.PaidAt >= previousMonthStart && p.PaidAt < monthStart)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        // ------------------------------------------------------------------
        // Service distribution. The donut dash values are pre-computed here so the
        // view only has to place them.
        // ------------------------------------------------------------------
        var typeCounts = await db.Connections
            .GroupBy(c => c.ConnectionType)
            .Select(group => new { Type = group.Key, Count = group.Count() })
            .ToListAsync();

        var totalConnections = typeCounts.Sum(entry => entry.Count);
        var distributionCounts = DistributionOrder
            .Select(type => typeCounts.FirstOrDefault(entry => entry.Type == type)?.Count ?? 0)
            .ToList();
        var donutSegments = ChartGeometry.DonutSegments(
            distributionCounts.Select(count => (double)count).ToList(),
            DonutRadius,
            gap: 6);
        var circumference = 2 * Math.PI * DonutRadius;

        var serviceDistribution = DistributionOrder
            .Select((type, index) => new
            {
                Type = type,
                Count = distributionCounts[index],
                Segment = donutSegments.Count > index ? donutSegments[index] : (Length: 0d, Offset: 0d)
            })
            .Where(entry => entry.Count > 0)
            .Select(entry => new ServiceDistributionSlice
            {
                Label = ConnectionTypeLabel(entry.Type),
                Count = entry.Count,
                Percent = totalConnections > 0 ? entry.Count * 100d / totalConnections : 0d,
                DashArray = $"{ChartGeometry.N(entry.Segment.Length)} {ChartGeometry.N(Math.Max(circumference - entry.Segment.Length, 0d))}",
                DashOffset = ChartGeometry.N(entry.Segment.Offset)
            })
            .ToList();

        // ------------------------------------------------------------------
        // Connection growth: new connections per month plus the running total.
        // The running total finishes on the true connection count.
        // ------------------------------------------------------------------
        var baselineConnections = totalConnections - monthlyConnections.Sum();
        var cumulativeConnections = MonthlySeries.Cumulative(monthlyConnections, baselineConnections);
        var monthLabels = Enumerable.Range(0, ChartMonths)
            .Select(offset => windowStart.AddMonths(offset).ToString("MMM"))
            .ToList();

        var connectionGrowth = monthLabels
            .Select((label, index) => new ConnectionGrowthPoint
            {
                Label = label,
                NewConnections = monthlyConnections[index],
                TotalConnections = cumulativeConnections[index]
            })
            .ToList();

        var model = new DashboardStatsViewModel
        {
            GreetingName = User.IsInRole("Admin") ? "Admin" : User.Identity?.Name ?? "Admin",
            PeriodLabel = $"{monthStart:d MMM yyyy} - {monthStart.AddMonths(1).AddDays(-1):d MMM yyyy}",

            CustomerGrowth = new DashboardMetric
            {
                Value = totalCustomers,
                PreviousValue = customersBeforeThisMonth,
                Trend = MonthlySeries.Tail(monthlyCustomers, TrendMonths)
            },
            ConnectionTrend = new DashboardMetric
            {
                Value = activeConnections,
                PreviousValue = activeConnectionsBeforeThisMonth,
                Trend = MonthlySeries.Tail(monthlyConnections, TrendMonths)
            },
            PendingOrderTrend = new DashboardMetric
            {
                Value = pendingOrders,
                PreviousValue = pendingOrdersBeforeThisMonth,
                Trend = MonthlySeries.Tail(monthlyOrders, TrendMonths)
            },
            RevenueTrend = new DashboardMetric
            {
                Value = (double)revenueThisMonth,
                PreviousValue = (double)revenueLastMonth,
                Trend = MonthlySeries.Tail(monthlyRevenue, TrendMonths)
            },

            RevenueThisMonth = revenueThisMonth,
            TotalConnections = totalConnections,
            ConnectionGrowth = connectionGrowth,
            ServiceDistribution = serviceDistribution,

            // The dashboard shows only the headline numbers; the remaining counters on
            // DashboardStatsViewModel belong to the other panels' dashboards.
            TotalCustomers = totalCustomers,
            ActiveConnections = activeConnections,
            PendingOrders = pendingOrders,

            RecentOrders = await db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .OrderByDescending(o => o.SubmittedAt)
                .Take(6)
                .ToListAsync()
        };

        return View(model);
    }

    private static string ConnectionTypeLabel(ConnectionType type) => type switch
    {
        ConnectionType.DialUp => "Dial-Up",
        ConnectionType.Broadband => "Broadband",
        ConnectionType.Telephone => "Telephone",
        _ => type.ToString()
    };
}
