using System.ComponentModel.DataAnnotations;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

public class TrackOrderViewModel
{
    [Required(ErrorMessage = "Enter your order number.")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "Order number must be exactly 11 characters (e.g. D0000000001).")]
    [Display(Name = "Order Number")]
    public string OrderNumber { get; set; } = string.Empty;

    public bool Searched { get; set; }
    public bool Found { get; set; }

    public string? FoundOrderNumber { get; set; }
    public ConnectionType? ConnectionType { get; set; }
    public OrderStatus? Status { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public string? City { get; set; }
    public string? PlanName { get; set; }
    public bool HasFeasibility { get; set; }
}
