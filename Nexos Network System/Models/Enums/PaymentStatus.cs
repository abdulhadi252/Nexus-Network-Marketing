namespace Nexos_Network_System.Models.Enums;

public enum PaymentStatus
{
    Recorded,
    Reversed,
    Cancelled,

    /// <summary>The customer submitted the payment from their panel; accounts still has to confirm it.</summary>
    PendingVerification
}
