using System.ComponentModel.DataAnnotations;

namespace Nexos_Network_System.Models.Enums;

public enum StockMovementType
{
    [Display(Name = "Stock In")]
    In,
    [Display(Name = "Stock Out")]
    Out,
    [Display(Name = "Adjustment Increase")]
    Adjustment,
    [Display(Name = "Adjustment Decrease")]
    AdjustmentDecrease
}
