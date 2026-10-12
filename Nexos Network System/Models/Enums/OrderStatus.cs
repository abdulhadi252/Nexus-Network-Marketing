namespace Nexos_Network_System.Models.Enums;

public enum OrderStatus
{
    Pending,
    FeasibilityPending,
    Feasible,
    NotFeasible,
    ConnectionProvided,
    Cancelled,

    /// <summary>Retail has verified the customer's details and forwarded the order to technical.</summary>
    VerifiedByRetail,

    /// <summary>Retail rejected the order; the recorded reason explains why.</summary>
    RetailRejected,

    /// <summary>Retail needs corrections from the customer before the order can proceed.</summary>
    AwaitingCustomerInformation
}
