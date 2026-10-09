using System.ComponentModel.DataAnnotations;
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

public class DashboardStatsViewModel
{
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
