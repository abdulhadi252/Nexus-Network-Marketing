using System.Data;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
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

        var assignedShopId = retailShopId ?? model.RetailShopId;

        // Customer self-placed orders arrive without a branch. Route them automatically to the
        // retail shop that serves the customer's primary address city, so the correct branch
        // can verify the customer and process the order.
        if (assignedShopId is null)
        {
            var addressCityId = await db.CustomerAddresses
                .AsNoTracking()
                .Where(a => a.CustomerId == model.CustomerId)
                .OrderByDescending(a => a.IsPrimary)
                .Select(a => (int?)a.CityId)
                .FirstOrDefaultAsync(cancellationToken);

            if (addressCityId.HasValue)
            {
                assignedShopId = await db.RetailShops
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.CityId == addressCityId.Value)
                    .OrderBy(s => s.Id)
                    .Select(s => (int?)s.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        if (assignedShopId.HasValue)
        {
            var shopCityId = await db.RetailShops
                .AsNoTracking()
                .Where(s => s.Id == assignedShopId.Value && s.IsActive)
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
            RetailShopId = assignedShopId,
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

        order.StatusHistory.Add(OrderWorkflow.CreateEntry(null, OrderStatus.Pending, processedByEmployeeId, "Order created."));

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var item in requestedItems)
        {
            await stockService.ReserveStockAsync(item.ProductId, item.Quantity, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return order.Id;
    }

    public async Task UpdateStatusAsync(int orderId, OrderStatus status, int? changedByEmployeeId = null, string? notes = null, CancellationToken cancellationToken = default)
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

        if (status == OrderStatus.VerifiedByRetail && reportStatus.HasValue)
        {
            throw new InvalidOperationException("This order already has a feasibility report, so it cannot be returned to retail verification.");
        }

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

        var previousStatus = order.Status;

        if (previousStatus != status && !OrderWorkflow.CanTransition(previousStatus, status))
        {
            throw new InvalidOperationException($"Order {order.OrderNumber} cannot move from {previousStatus} to {status}.");
        }

        if (status == OrderStatus.Cancelled && previousStatus != OrderStatus.Cancelled)
        {
            foreach (var item in order.Items)
            {
                await stockService.ReleaseReservedStockAsync(item.ProductId, item.Quantity, cancellationToken);
            }
        }

        order.Status = status;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        if (previousStatus != status)
        {
            order.StatusHistory.Add(OrderWorkflow.CreateEntry(previousStatus, status, changedByEmployeeId, notes));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task VerifyByRetailAsync(int orderId, int employeeId, string? notes, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("A cancelled order cannot be verified.");
        }

        if (order.Status is OrderStatus.Feasible or OrderStatus.NotFeasible or OrderStatus.ConnectionProvided)
        {
            throw new InvalidOperationException("This order has already moved past retail verification.");
        }

        if (await db.Connections.AnyAsync(connection => connection.OrderId == orderId, cancellationToken))
        {
            throw new InvalidOperationException("This order already has a connection and cannot be re-verified.");
        }

        if (!OrderWorkflow.CanTransition(order.Status, OrderStatus.VerifiedByRetail))
        {
            throw new InvalidOperationException($"Order {order.OrderNumber} cannot be verified from its current status.");
        }

        var previousStatus = order.Status;
        order.Status = OrderStatus.VerifiedByRetail;
        order.VerifiedByEmployeeId = employeeId;
        order.VerifiedAt = DateTimeOffset.UtcNow;
        order.VerificationNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.StatusHistory.Add(OrderWorkflow.CreateEntry(previousStatus, OrderStatus.VerifiedByRetail, employeeId, notes));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task RejectByRetailAsync(int orderId, int employeeId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A rejection reason is required.");
        }

        return ApplyRetailDecisionAsync(orderId, OrderStatus.RetailRejected, employeeId, reason, cancellationToken);
    }

    public Task RequestCustomerInformationAsync(int orderId, int employeeId, string? notes, CancellationToken cancellationToken = default) =>
        ApplyRetailDecisionAsync(orderId, OrderStatus.AwaitingCustomerInformation, employeeId, notes, cancellationToken);

    private async Task ApplyRetailDecisionAsync(int orderId, OrderStatus decision, int employeeId, string? notes, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");

        if (order.Status is OrderStatus.Feasible or OrderStatus.NotFeasible or OrderStatus.ConnectionProvided or OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("This order has already moved past retail review.");
        }

        if (!OrderWorkflow.CanTransition(order.Status, decision))
        {
            throw new InvalidOperationException($"Order {order.OrderNumber} cannot move from {order.Status} to {decision}.");
        }

        var previousStatus = order.Status;
        order.Status = decision;
        order.VerifiedByEmployeeId = employeeId;
        order.VerifiedAt = DateTimeOffset.UtcNow;
        order.VerificationNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.StatusHistory.Add(OrderWorkflow.CreateEntry(previousStatus, decision, employeeId, notes));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
