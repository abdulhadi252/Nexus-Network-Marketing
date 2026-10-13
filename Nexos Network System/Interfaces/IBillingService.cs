using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Interfaces;

public interface IBillingService
{
    /// <summary>Service tax percentage applied on every generated bill (12.24%).</summary>
    const decimal ServiceTaxRate = 0.1224m;

    Task<Bill> GenerateBillAsync(BillGenerateViewModel model, CancellationToken cancellationToken = default);
}
