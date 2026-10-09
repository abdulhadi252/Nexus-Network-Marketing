namespace Nexos_Network_System.Helpers;

public static class StatusBadges
{
    public static string CssClass(object? value) => (value?.ToString() ?? string.Empty) switch
    {
        "Pending" or "Unpaid" or "FeasibilityPending" or "Available" or "In" => "badge bg-warning text-dark",
        "Active" or "Paid" or "Feasible" or "Recorded" or "ConnectionProvided" or "Completed" or "Assigned" => "badge bg-success",
        "TemporarilyInactive" or "PartiallyPaid" or "InProgress" or "UnderRepair" or "Out" => "badge bg-info text-dark",
        "NotFeasible" or "PermanentlyInactive" or "Cancelled" or "Reversed" or "Rejected" or "Retired" or "Adjustment" => "badge bg-danger",
        _ => "badge bg-secondary"
    };
}
