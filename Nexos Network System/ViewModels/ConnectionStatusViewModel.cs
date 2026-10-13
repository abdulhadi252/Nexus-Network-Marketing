using System.ComponentModel.DataAnnotations;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

public class ConnectionStatusViewModel
{
    [Required(ErrorMessage = "Enter your account number.")]
    [StringLength(16, MinimumLength = 16, ErrorMessage = "Account number must be exactly 16 characters (e.g. B001000000000001).")]
    [Display(Name = "Account Number")]
    public string AccountNumber { get; set; } = string.Empty;

    public bool Searched { get; set; }
    public bool Found { get; set; }

    public string? FoundAccountNumber { get; set; }
    public ConnectionType? ConnectionType { get; set; }
    public ConnectionStatus? Status { get; set; }
    public string? PlanName { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }
    public string? City { get; set; }
}
