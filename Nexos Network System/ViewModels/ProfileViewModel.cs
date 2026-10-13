using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

public class ProfileViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Roles { get; set; }

    public Employee? Employee { get; set; }
    public Customer? Customer { get; set; }
    public IReadOnlyList<Connection> CustomerConnections { get; set; } = new List<Connection>();
}

public class ConnectionCreateViewModel
{
    [Required, Display(Name = "Feasible Order")]
    public int OrderId { get; set; }

    [StringLength(2000)]
    [Display(Name = "Installation Notes")]
    public string? InstallationNotes { get; set; }

    [Display(Name = "Activate Immediately")]
    public bool ActivateImmediately { get; set; } = true;
}

public class EquipmentAssignViewModel
{
    [Required, Display(Name = "Equipment")]
    public int EquipmentId { get; set; }

    [Required, Display(Name = "Connection")]
    public int ConnectionId { get; set; }
}

/// <summary>A single entry in the panel topbar notification menu.</summary>
public class PanelNotification
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }

    /// <summary>Where the entry links to; null when the count has no dedicated page.</summary>
    public string? Url { get; set; }
}

/// <summary>Identity of the signed-in user as shown in the panel topbar chip.</summary>
public class PanelUserViewModel
{
    public string DisplayName { get; set; } = "Account";
    public string Initial { get; set; } = "?";
    public string RoleLabel { get; set; } = "User";
}

/// <summary>Backs the topbar bell: real counts for the signed-in user's panel.</summary>
public class PanelNotificationsViewModel
{
    public IReadOnlyList<PanelNotification> Items { get; set; } = new List<PanelNotification>();

    public int TotalCount => Items.Sum(item => item.Count);

    public bool HasItems => Items.Count > 0;
}

/// <summary>
/// One headline number plus the context needed to render it as a KPI card: the value, a
/// month-over-month comparison and a short series for the sparkline.
/// </summary>
public class DashboardMetric
{
    public double Value { get; set; }

    /// <summary>The same metric measured for the previous month; 0 when there is no baseline.</summary>
    public double PreviousValue { get; set; }

    /// <summary>Recent months of the underlying series, oldest first, used for the sparkline.</summary>
    public IReadOnlyList<double> Trend { get; set; } = new List<double>();

    public bool HasTrend => Trend.Count > 1;

    public bool IsUp => ChangePercent >= 0;

    /// <summary>Month-over-month change; treated as full growth when there is no previous baseline.</summary>
    public double ChangePercent => PreviousValue > 0
        ? ((Value - PreviousValue) / PreviousValue) * 100d
        : (Value > 0 ? 100d : 0d);

    public string ChangeLabel => string.Format(
        CultureInfo.InvariantCulture,
        "{0}{1:0.#}%",
        ChangePercent >= 0 ? "+" : "-",
        Math.Abs(ChangePercent));
}

/// <summary>A single month on the connection growth chart.</summary>
public class ConnectionGrowthPoint
{
    public string Label { get; set; } = string.Empty;
    public int NewConnections { get; set; }
    public int TotalConnections { get; set; }
}

/// <summary>A single slice of the service distribution donut, including its pre-computed SVG dash values.</summary>
public class ServiceDistributionSlice
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percent { get; set; }

    public string DashArray { get; set; } = string.Empty;
    public string DashOffset { get; set; } = string.Empty;

    public string PercentLabel => string.Format(CultureInfo.InvariantCulture, "{0:0.#}%", Percent);
}

public class DashboardStatsViewModel
{
    /// <summary>Name used in the "Welcome back" greeting.</summary>
    public string GreetingName { get; set; } = "Admin";

    /// <summary>Human-readable range the headline numbers cover (the current calendar month).</summary>
    public string PeriodLabel { get; set; } = string.Empty;

    public DashboardMetric CustomerGrowth { get; set; } = new();
    public DashboardMetric ConnectionTrend { get; set; } = new();
    public DashboardMetric PendingOrderTrend { get; set; } = new();
    public DashboardMetric RevenueTrend { get; set; } = new();

    public decimal RevenueThisMonth { get; set; }

    public int TotalConnections { get; set; }
    public IReadOnlyList<ConnectionGrowthPoint> ConnectionGrowth { get; set; } = new List<ConnectionGrowthPoint>();
    public IReadOnlyList<ServiceDistributionSlice> ServiceDistribution { get; set; } = new List<ServiceDistributionSlice>();

    public int TotalCustomers { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ActiveConnections { get; set; }
    public int PendingConnections { get; set; }
    public int TotalPlans { get; set; }
    public int OutstandingBills { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal PaymentsReceived { get; set; }
    public int LowStockItems { get; set; }
    public int PendingFeasibility { get; set; }
    public int ApprovedFeasibility { get; set; }
    public int NotFeasibleCount { get; set; }
    public int TotalRetailShops { get; set; }
    public int TotalVendors { get; set; }
    public int TotalProducts { get; set; }
    public int TotalBills { get; set; }
    public int DueBills { get; set; }
    public int PaidBills { get; set; }
    public int PartiallyPaidBills { get; set; }
    public int TotalEquipment { get; set; }
    public int AvailableEquipment { get; set; }
    public int OpenFeedback { get; set; }

    public IReadOnlyList<Order> RecentOrders { get; set; } = new List<Order>();
    public IReadOnlyList<Payment> RecentPayments { get; set; } = new List<Payment>();
    public IReadOnlyList<Stock> LowStock { get; set; } = new List<Stock>();
    public IReadOnlyList<Bill> DueBillsList { get; set; } = new List<Bill>();
    public IReadOnlyList<FeasibilityReport> PendingFeasibilityList { get; set; } = new List<FeasibilityReport>();
    public IReadOnlyList<Connection> PendingConnectionsList { get; set; } = new List<Connection>();
}
