using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Interfaces;

public interface IOrderService
{
    Task<int> CreateOrderAsync(OrderCreateViewModel model, int? retailShopId, int? processedByEmployeeId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(int orderId, OrderStatus status, int? changedByEmployeeId = null, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>Marks the order as verified by the retail branch and forwards it to technical.</summary>
    Task VerifyByRetailAsync(int orderId, int employeeId, string? notes, CancellationToken cancellationToken = default);

    /// <summary>Retail rejects the order and records the reason.</summary>
    Task RejectByRetailAsync(int orderId, int employeeId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Retail asks the customer for corrections / missing information.</summary>
    Task RequestCustomerInformationAsync(int orderId, int employeeId, string? notes, CancellationToken cancellationToken = default);
}
