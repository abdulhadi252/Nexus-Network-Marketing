using System.ComponentModel.DataAnnotations;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

public class PlanFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Connection Type")]
    [EnumDataType(typeof(ConnectionType))]
    public ConnectionType ConnectionType { get; set; } = ConnectionType.Broadband;

    [Display(Name = "Billing Cycle")]
    [EnumDataType(typeof(PlanBillingCycle))]
    public PlanBillingCycle BillingCycle { get; set; } = PlanBillingCycle.Monthly;

    [Range(1, 3650)]
    [Display(Name = "Validity (Days)")]
    public int? ValidityDays { get; set; }

    [Range(1, 100000)]
    [Display(Name = "Speed (Mbps)")]
    public int? SpeedMbps { get; set; }

    [Range(1, 10000)]
    [Display(Name = "Included Usage (Hours)")]
    public int? IncludedUsageHours { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Recurring Charge")]
    public decimal RecurringCharge { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Security Deposit")]
    public decimal SecurityDeposit { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Installation Charge")]
    public decimal InstallationCharge { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
