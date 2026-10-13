using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Models;

public class FeasibilityReport
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int? TechnicalEmployeeId { get; set; }
    public Employee? TechnicalEmployee { get; set; }
    public FeasibilityStatus Status { get; set; } = FeasibilityStatus.Pending;
    public decimal? DistanceMeters { get; set; }
    public bool? ServerAvailable { get; set; }
    public string? Remarks { get; set; }
    public DateTimeOffset? CheckedAt { get; set; }
}
