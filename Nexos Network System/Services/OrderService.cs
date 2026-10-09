using System.Data;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Services;

public class OrderService(ApplicationDbContext db, IOrderIdService orderIdService, IStockService stockService) : IOrderService
{
    public async Task<int> CreateOrderAsync(OrderCreateViewModel model, int? retailShopId, int? processedByEmployeeId, CancellationToken cancellationToken = default)
    {
        var customerExists = await db.Customers.AnyAsync(c => c.Id == model.CustomerId && c.IsActive, cancellationToken);
        if (!customerExists)
        {
            throw new InvalidOperationException("The selected customer is not available.");
        }

        if (retailShopId.HasValue)
        {
            var shopCityId = await db.RetailShops
                .AsNoTracking()
                .Where(s => s.Id == retailShopId.Value && s.IsActive)
                .Select(s => (int?)s.CityId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("The assigned retail shop is not available.");

            if (!await db.CustomerAddresses.AnyAsync(
                    address => address.CustomerId == model.CustomerId && address.CityId == shopCityId,
                    cancellationToken))
            {
                throw new InvalidOperationException("This customer is outside the assigned retail shop's service area.");
            }
        }

        var plan = await db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == model.PlanId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("The selected plan is not available.");

        if (plan.ConnectionType != model.ConnectionType)
        {
            throw new InvalidOperationException("The selected plan does not match the chosen connection type.");
        }

        var requestedItems = model.Items
            .Where(i => i.ProductId > 0 && i.Quantity > 0)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        foreach (var item in requestedItems)
        {
            if (!await db.Products.AnyAsync(p => p.Id == item.ProductId && p.IsActive, cancellationToken))
            {
                throw new InvalidOperationException("The selected product is not available.");
            }

            if (!await stockService.HasAvailableStockAsync(item.ProductId, item.Quantity, cancellationToken))
            {
                var productName = await db.Products.Where(p => p.Id == item.ProductId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken) ?? "Product";
                throw new InvalidOperationException($"Not enough available stock for '{productName}'.");
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var (orderNumber, sequenceNumber) = await orderIdService.GetNextOrderNumberAsync(model.ConnectionType, cancellationToken);

        var order = new Order
        {
            OrderNumber = orderNumber,
            SequenceNumber = sequenceNumber,
            ConnectionType = model.ConnectionType,
            Status = OrderStatus.Pending,
            CustomerId = model.CustomerId,
            RetailShopId = retailShopId ?? model.RetailShopId,
            ProcessedByEmployeeId = processedByEmployeeId,
            PlanId = model.PlanId,
            SubmittedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Remarks = model.Notes
        };

        foreach (var item in requestedItems)
        {
            var product = await db.Products.AsNoTracking().FirstAsync(p => p.Id == item.ProductId, cancellationToken);
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.UnitSalePrice ?? product.UnitCost
            });
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var item in requestedItems)
        {
            await stockService.ReserveStockAsync(item.ProductId, item.Quantity, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return order.Id;
    }

    public async Task UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(status))
        {
            throw new InvalidOperationException("Choose a valid order status.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");

        if (order.Status == OrderStatus.Cancelled && status != OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("A cancelled order cannot be reopened.");
        }

        var reportStatus = await db.FeasibilityReports
            .Where(report => report.OrderId == orderId)
            .Select(report => (FeasibilityStatus?)report.Status)
            .FirstOrDefaultAsync(cancellationToken);

        if (status is OrderStatus.Pending or OrderStatus.Feasible or OrderStatus.NotFeasible or OrderStatus.FeasibilityPending)
        {
            var matchingOrderStatus = reportStatus switch
            {
                FeasibilityStatus.Feasible => OrderStatus.Feasible,
                FeasibilityStatus.NotFeasible => OrderStatus.NotFeasible,
                FeasibilityStatus.Pending => OrderStatus.FeasibilityPending,
                _ => OrderStatus.Pending
            };

            if ((reportStatus.HasValue && status != matchingOrderStatus) ||
                (!reportStatus.HasValue && status is OrderStatus.Feasible or OrderStatus.NotFeasible or OrderStatus.FeasibilityPending))
            {
                throw new InvalidOperationException("Order status must match its recorded feasibility report.");
            }
        }

        var hasConnection = await db.Connections.AnyAsync(connection => connection.OrderId == orderId, cancellationToken);
        if ((status == OrderStatus.ConnectionProvided && !hasConnection) ||
            (hasConnection && status != OrderStatus.ConnectionProvided))
        {
            throw new InvalidOperationException("Order status must remain connected once its connection has been created.");
        }

        if (status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
        {
            foreach (var item in order.Items)
            {
                await stockService.ReleaseReservedStockAsync(item.ProductId, item.Quantity, cancellationToken);
            }
        }

        order.Status = status;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
