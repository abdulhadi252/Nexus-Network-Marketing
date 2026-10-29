using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

/// <summary>
/// One piece of equipment as the customer sees it, plus the cancellation/return requests they
/// have already sent for it.
/// </summary>
public class CustomerEquipmentDetailsViewModel
{
    public Equipment Equipment { get; set; } = null!;

    public IReadOnlyList<EquipmentRequest> Requests { get; set; } = new List<EquipmentRequest>();

    public EquipmentRequest? PendingRequest =>
        Requests.FirstOrDefault(r => r.Status == EquipmentRequestStatus.Pending);

    /// <summary>Only assigned equipment without an open request can be cancelled or returned.</summary>
    public bool CanRequest => Equipment.Status == EquipmentStatus.Assigned && PendingRequest is null;
}
