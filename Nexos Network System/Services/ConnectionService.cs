using System.Data;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Services;

public class ConnectionService(ApplicationDbContext db, IOrderIdService orderIdService) : IConnectionService
{
    public async Task<int> CreateFromOrderAsync(ConnectionCreateViewModel model, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var order = await db.Orders
            .Include(o => o.Customer).ThenInclude(c => c.Addresses)
            .Include(o => o.Connection)
            .FirstOrDefaultAsync(o => o.Id == model.OrderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");

        if (order.Connection is not null)
        {
            throw new InvalidOperationException("This order already has a connection.");
        }

        if (order.Status != OrderStatus.Feasible)
        {
            throw new InvalidOperationException("Only orders with a confirmed feasible report can be converted into connections.");
        }

        if (!await db.FeasibilityReports.AnyAsync(
                report => report.OrderId == order.Id && report.Status == FeasibilityStatus.Feasible,
                cancellationToken))
        {
            throw new InvalidOperationException("A feasible technical report is required before creating a connection.");
        }

        if (model.InstallationNotes?.Length > 2000)
        {
            throw new InvalidOperationException("Installation notes cannot exceed 2,000 characters.");
        }

        if (order.PlanId is null)
        {
            throw new InvalidOperationException("The order has no plan assigned.");
        }

        var cityId = order.RetailShopId.HasValue
            ? await db.RetailShops
                .Where(shop => shop.Id == order.RetailShopId.Value)
                .Select(shop => (int?)shop.CityId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("The order's assigned retail shop no longer exists.")
            : order.Customer.Addresses
                .OrderByDescending(address => address.IsPrimary)
                .Select(address => (int?)address.CityId)
                .FirstOrDefault()
                ?? await db.Cities.Select(city => (int?)city.Id).FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("No city is configured for connection creation.");

        var (accountNumber, serialNumber) = await orderIdService.GetNextAccountNumberAsync(order.ConnectionType, cityId, cancellationToken);

        var connection = new Connection
        {
            AccountNumber = accountNumber,
            AccountSerialNumber = serialNumber,
            ConnectionType = order.ConnectionType,
            Status = model.ActivateImmediately ? ConnectionStatus.Active : ConnectionStatus.Pending,
            CustomerId = order.CustomerId,
            CityId = cityId,
            PlanId = order.PlanId.Value,
            OrderId = order.Id,
            InstalledAt = DateTimeOffset.UtcNow,
            ActivatedAt = model.ActivateImmediately ? DateTimeOffset.UtcNow : null,
            InstallationNotes = model.InstallationNotes
        };

        db.Connections.Add(connection);

        order.Status = OrderStatus.ConnectionProvided;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        db.ConnectionPlanHistories.Add(new ConnectionPlanHistory
        {
            Connection = connection,
            PlanId = order.PlanId.Value,
            EffectiveFrom = DateTimeOffset.UtcNow,
            Reason = "Initial plan assigned with connection."
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return connection.Id;
    }

    public async Task UpdateStatusAsync(int connectionId, ConnectionStatus status, string? notes, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(status))
        {
            throw new InvalidOperationException("Choose a valid connection status.");
        }

        if (notes?.Length > 2000)
        {
            throw new InvalidOperationException("Installation notes cannot exceed 2,000 characters.");
        }

        var connection = await db.Connections.FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken)
            ?? throw new InvalidOperationException("Connection not found.");

        if (connection.Status is ConnectionStatus.PermanentlyInactive or ConnectionStatus.Cancelled &&
            status != connection.Status)
        {
            throw new InvalidOperationException("A permanently inactive or cancelled connection cannot be reactivated.");
        }

        connection.Status = status;

        if (status == ConnectionStatus.Active && connection.ActivatedAt is null)
        {
            connection.ActivatedAt = DateTimeOffset.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            connection.InstallationNotes = notes;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
