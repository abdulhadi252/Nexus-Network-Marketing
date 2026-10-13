using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Interfaces;

public interface IPaymentService
{
    Task<Payment> RecordPaymentAsync(PaymentRecordViewModel model, int? recordedByEmployeeId, CancellationToken cancellationToken = default);
}
