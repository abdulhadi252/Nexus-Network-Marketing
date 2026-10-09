using System.ComponentModel.DataAnnotations;

namespace Nexos_Network_System.ViewModels;

public class BillGenerateViewModel
{
    [Required, Display(Name = "Connection")]
    public int ConnectionId { get; set; }

    [Required, Display(Name = "Billing Period Start")]
    [DataType(DataType.Date)]
    public DateOnly BillingPeriodStart { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);

    [Required, Display(Name = "Billing Period End")]
    [DataType(DataType.Date)]
    public DateOnly BillingPeriodEnd { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Range(0, 9999999)]
    [Display(Name = "Discount Amount")]
    public decimal DiscountAmount { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Scheme Discount Amount")]
    public decimal SchemeDiscountAmount { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Equipment Charges")]
    public decimal EquipmentCharges { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Replacement Charges")]
    public decimal ReplacementCharges { get; set; }
}
