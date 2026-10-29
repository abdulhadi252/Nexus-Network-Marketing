using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Models;

/// <summary>
/// A request the customer raises from their panel against equipment assigned to their connection.
/// Technical reviews it and either approves it (the equipment goes back to inventory) or rejects it.
/// Equipment is accepted automatically when it is assigned, so there is no acceptance request.
/// </summary>
public class EquipmentRequest
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public EquipmentRequestType RequestType { get; set; }

    public EquipmentRequestStatus Status { get; set; } = EquipmentRequestStatus.Pending;

    /// <summary>The customer's own words about why the equipment is being cancelled or returned.</summary>
    public string? Reason { get; set; }

    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReviewedAt { get; set; }

    public int? ReviewedByEmployeeId { get; set; }
    public Employee? ReviewedByEmployee { get; set; }

    public string? ReviewNotes { get; set; }
}
