using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Helpers;

public static class OrderWorkflow
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.VerifiedByRetail, OrderStatus.AwaitingCustomerInformation, OrderStatus.RetailRejected, OrderStatus.FeasibilityPending, OrderStatus.Cancelled },
        [OrderStatus.AwaitingCustomerInformation] = new[] { OrderStatus.VerifiedByRetail, OrderStatus.RetailRejected, OrderStatus.Cancelled },
        [OrderStatus.VerifiedByRetail] = new[] { OrderStatus.FeasibilityPending, OrderStatus.Feasible, OrderStatus.NotFeasible, OrderStatus.Cancelled },
        [OrderStatus.FeasibilityPending] = new[] { OrderStatus.Feasible, OrderStatus.NotFeasible, OrderStatus.Cancelled },
        [OrderStatus.Feasible] = new[] { OrderStatus.ConnectionProvided, OrderStatus.NotFeasible, OrderStatus.Cancelled },
        [OrderStatus.NotFeasible] = new[] { OrderStatus.Pending, OrderStatus.Cancelled },
        [OrderStatus.RetailRejected] = new[] { OrderStatus.Pending, OrderStatus.Cancelled },
        [OrderStatus.ConnectionProvided] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        from == to || (AllowedTransitions.TryGetValue(from, out var targets) && targets.Contains(to));

    public static OrderStatusHistory CreateEntry(OrderStatus? from, OrderStatus to, int? changedByEmployeeId, string? notes) =>
        new()
        {
            FromStatus = from,
            ToStatus = to,
            ChangedByEmployeeId = changedByEmployeeId,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            ChangedAt = DateTimeOffset.UtcNow
        };
}
