using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Interfaces;

public interface IOrderService
{
    Task<int> CreateOrderAsync(OrderCreateViewModel model, int? retailShopId, int? processedByEmployeeId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken cancellationToken = default);
}
