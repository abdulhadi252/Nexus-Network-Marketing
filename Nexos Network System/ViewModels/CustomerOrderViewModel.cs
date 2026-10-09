using System.ComponentModel.DataAnnotations;

namespace Nexos_Network_System.ViewModels;

public class CustomerOrderViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose an available plan.")]
    [Display(Name = "Plan")]
    public int PlanId { get; set; }

    [StringLength(2000)]
    [Display(Name = "Notes / installation details")]
    public string? Notes { get; set; }
}
