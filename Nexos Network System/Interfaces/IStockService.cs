using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Interfaces;

public interface IStockService
{
    Task RecordMovementAsync(int productId, StockMovementType movementType, int quantity, string? reason, int? performedByEmployeeId, CancellationToken cancellationToken = default);
    Task<bool> HasAvailableStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
    Task ReserveStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
    Task ReleaseReservedStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
}
