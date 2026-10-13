using System.ComponentModel.DataAnnotations;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

public class StockAdjustViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int ReorderLevel { get; set; }

    [Required, EnumDataType(typeof(StockMovementType)), Display(Name = "Movement Type")]
    public StockMovementType MovementType { get; set; } = StockMovementType.In;

    [Range(1, 1000000, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; } = 1;

    [StringLength(500)]
    public string? Reason { get; set; }
}
