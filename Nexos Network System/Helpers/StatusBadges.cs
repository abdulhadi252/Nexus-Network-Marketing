namespace Nexos_Network_System.Helpers;

public static class StatusBadges
{
    /// <summary>
    /// Dot-and-label pill styling used by list tables, where a softer treatment reads
    /// better than the solid badge. Mirrors the same status vocabulary as
    /// <see cref="CssClass"/> so a status is always rendered in the same colour family.
    /// </summary>
    public static string PillCssClass(object? value) => (value?.ToString() ?? string.Empty) switch
    {
        "Active" or "Paid" or "Feasible" or "Recorded" or "ConnectionProvided" or "Completed" or "Assigned" or "Available" or "In" or "Approved"
            => "status-pill status-pill--positive",
        "Pending" or "Unpaid" or "FeasibilityPending" or "AwaitingCustomerInformation" or "PartiallyPaid" or "InProgress" or "PendingVerification"
            => "status-pill status-pill--warning",
        "TemporarilyInactive" or "UnderRepair" or "VerifiedByRetail" or "Out"
            => "status-pill status-pill--info",
        "NotFeasible" or "RetailRejected" or "PermanentlyInactive" or "Cancelled" or "Reversed" or "Rejected" or "Retired"
            => "status-pill status-pill--danger",
        "Inactive" or "Adjustment"
            => "status-pill status-pill--neutral",
        _ => "status-pill status-pill--neutral"
    };

    public static string CssClass(object? value) => (value?.ToString() ?? string.Empty) switch
    {
        "Pending" or "Unpaid" or "FeasibilityPending" or "AwaitingCustomerInformation" or "Available" or "In" or "PendingVerification" => "badge bg-warning text-dark",
        "Active" or "Paid" or "Feasible" or "Recorded" or "ConnectionProvided" or "Completed" or "Assigned" or "Approved" => "badge bg-success",
        "TemporarilyInactive" or "PartiallyPaid" or "InProgress" or "UnderRepair" or "Out" or "VerifiedByRetail" => "badge bg-info text-dark",
        "NotFeasible" or "RetailRejected" or "PermanentlyInactive" or "Cancelled" or "Reversed" or "Rejected" or "Retired" or "Adjustment" => "badge bg-danger",
        _ => "badge bg-secondary"
    };
}
