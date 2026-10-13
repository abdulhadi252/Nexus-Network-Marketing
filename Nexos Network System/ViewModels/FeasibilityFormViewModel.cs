using System.ComponentModel.DataAnnotations;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

public class FeasibilityFormViewModel
{
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    [Display(Name = "Feasibility Status")]
    [EnumDataType(typeof(FeasibilityStatus))]
    public FeasibilityStatus Status { get; set; } = FeasibilityStatus.Feasible;

    [Range(0, 9999999)]
    [Display(Name = "Distance from Server (Meters)")]
    public decimal? DistanceMeters { get; set; }

    [Display(Name = "Server / Network Available")]
    public bool? ServerAvailable { get; set; }

    [StringLength(2000)]
    public string? Remarks { get; set; }
}
