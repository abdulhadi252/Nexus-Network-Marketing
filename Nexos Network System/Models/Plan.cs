using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Models;

public class Plan
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ConnectionType ConnectionType { get; set; }
    public PlanBillingCycle BillingCycle { get; set; }
    public int? ValidityDays { get; set; }
    public int? SpeedMbps { get; set; }
    public int? IncludedUsageHours { get; set; }
    public decimal RecurringCharge { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal InstallationCharge { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Connection> Connections { get; set; } = new List<Connection>();
    public ICollection<ConnectionPlanHistory> ConnectionPlanHistories { get; set; } = new List<ConnectionPlanHistory>();
}
