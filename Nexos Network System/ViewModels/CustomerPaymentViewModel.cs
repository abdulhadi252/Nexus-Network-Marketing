using System.ComponentModel.DataAnnotations;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

/// <summary>
/// A bill payment started from the customer panel. The customer picks one of their own
/// outstanding bills, chooses a billing method and submits the transaction reference;
/// accounts then confirms the money and the bill is updated.
/// </summary>
public class CustomerPaymentViewModel
{
    [Required(ErrorMessage = "Choose the bill you want to pay.")]
    public int BillId { get; set; }

    public string BillNumber { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;

    [Display(Name = "Outstanding due")]
    public decimal DueAmount { get; set; }

    [Range(0.01, 9999999, ErrorMessage = "Enter an amount between 0.01 and 9,999,999.")]
    [Display(Name = "Amount to pay")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Choose a billing method.")]
    [EnumDataType(typeof(PaymentMethod), ErrorMessage = "Choose a valid billing method.")]
    [Display(Name = "Billing method")]
    public PaymentMethod Method { get; set; } = PaymentMethod.Card;

    [StringLength(150, ErrorMessage = "The transaction reference cannot exceed 150 characters.")]
    [Display(Name = "Transaction / cheque number")]
    public string? PaymentReference { get; set; }

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    /// <summary>Every outstanding bill this customer can pay right now.</summary>
    public IReadOnlyList<Bill> OpenBills { get; set; } = new List<Bill>();
}
