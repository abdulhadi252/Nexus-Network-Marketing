using System.ComponentModel.DataAnnotations;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

public class PaymentRecordViewModel
{
    [Required]
    public int BillId { get; set; }

    public string BillNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public decimal DueAmount { get; set; }

    [Range(0.01, 9999999, ErrorMessage = "Enter an amount greater than zero.")]
    public decimal Amount { get; set; }

    [Display(Name = "Payment Method")]
    [EnumDataType(typeof(PaymentMethod))]
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    [StringLength(150)]
    [Display(Name = "Payment Reference")]
    public string? PaymentReference { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
