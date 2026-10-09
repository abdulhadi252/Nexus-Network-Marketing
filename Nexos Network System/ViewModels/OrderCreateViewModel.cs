using System.ComponentModel.DataAnnotations;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

public class OrderItemInputViewModel
{
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Range(0, 1000)]
    public int Quantity { get; set; }
}

public class OrderCreateViewModel
{
    [Required, Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Display(Name = "Connection Type")]
    [EnumDataType(typeof(ConnectionType))]
    public ConnectionType ConnectionType { get; set; } = ConnectionType.Broadband;

    [Display(Name = "Retail Shop")]
    public int? RetailShopId { get; set; }

    [Required, Display(Name = "Plan")]
    public int PlanId { get; set; }

    [StringLength(2000)]
    [Display(Name = "Notes / Remarks")]
    public string? Notes { get; set; }

    public List<OrderItemInputViewModel> Items { get; set; } = new();
}
