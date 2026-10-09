using System.Data;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Services;

public class StockService(ApplicationDbContext db) : IStockService
{
    public async Task<bool> HasAvailableStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return true;
        }

        var stock = await db.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken);
        if (stock is null)
        {
            return quantity == 0;
        }

        return stock.QuantityOnHand - stock.QuantityReserved >= quantity;
    }

    public async Task ReserveStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return;
        }

        var stock = await db.StockItems.FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken);
        if (stock is null)
        {
            throw new InvalidOperationException("No stock record exists for this product.");
        }

        if (stock.QuantityOnHand - stock.QuantityReserved < quantity)
        {
            throw new InvalidOperationException("Not enough available stock to reserve.");
        }

        stock.QuantityReserved += quantity;
        stock.LastUpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseReservedStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return;
        }

        var stock = await db.StockItems.FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken)
            ?? throw new InvalidOperationException("No stock record exists for this product.");

        if (stock.QuantityReserved < quantity)
        {
            throw new InvalidOperationException("Reserved stock is lower than the quantity being released.");
        }

        stock.QuantityReserved -= quantity;
        stock.LastUpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordMovementAsync(int productId, StockMovementType movementType, int quantity, string? reason, int? performedByEmployeeId, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(movementType))
        {
            throw new InvalidOperationException("Choose a valid stock movement type.");
        }

        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        if (quantity > 1_000_000 || reason?.Length > 500)
        {
            throw new InvalidOperationException("Stock movement quantity or reason exceeds the allowed limit.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var stock = await db.StockItems.FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken)
            ?? throw new InvalidOperationException("No stock record exists for this product.");

        var signedChange = movementType switch
        {
            StockMovementType.In => quantity,
            StockMovementType.Out or StockMovementType.AdjustmentDecrease => -quantity,
            _ => quantity
        };

        if (movementType == StockMovementType.Out && stock.QuantityOnHand - stock.QuantityReserved < quantity)
        {
            throw new InvalidOperationException("Cannot issue more stock than is available on hand.");
        }

        if (movementType is StockMovementType.Adjustment or StockMovementType.AdjustmentDecrease)
        {
            var newValue = stock.QuantityOnHand + signedChange;
            if (newValue < stock.QuantityReserved)
            {
                throw new InvalidOperationException("Adjustment cannot reduce stock below the quantity reserved for orders.");
            }
        }

        stock.QuantityOnHand += signedChange;

        if (stock.QuantityOnHand < 0)
        {
            stock.QuantityOnHand = 0;
        }

        stock.LastUpdatedAt = DateTimeOffset.UtcNow;

        db.StockMovements.Add(new StockMovement
        {
            StockId = stock.Id,
            MovementType = movementType,
            QuantityChange = signedChange,
            QuantityAfter = stock.QuantityOnHand,
            Reason = reason,
            PerformedByEmployeeId = performedByEmployeeId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
