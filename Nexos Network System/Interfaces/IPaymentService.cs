using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Interfaces;

public interface IPaymentService
{
    Task<Payment> RecordPaymentAsync(PaymentRecordViewModel model, int? recordedByEmployeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a payment the customer submitted from the customer panel. The money is not
    /// applied to the bill yet - it waits in <see cref="Models.Enums.PaymentStatus.PendingVerification"/>
    /// until the accounts team confirms it.
    /// </summary>
    Task<Payment> SubmitOnlinePaymentAsync(CustomerPaymentViewModel model, int customerId, CancellationToken cancellationToken = default);

    /// <summary>Confirms a customer-submitted payment and applies it to its bill.</summary>
    Task<Payment> VerifySubmittedPaymentAsync(int paymentId, int employeeId, string? notes, CancellationToken cancellationToken = default);

    /// <summary>Rejects a customer-submitted payment; the bill keeps its outstanding balance.</summary>
    Task<Payment> RejectSubmittedPaymentAsync(int paymentId, int employeeId, string reason, CancellationToken cancellationToken = default);
}
