using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Services;

/// <summary>
/// Central generator for the two specification identifiers. Unique database indexes on
/// OrderNumber / AccountNumber are the final safety net against duplicates.
/// </summary>
public class OrderIdService(ApplicationDbContext db) : IOrderIdService
{
    public static string PrefixFor(ConnectionType connectionType) => connectionType switch
    {
        ConnectionType.DialUp => "D",
        ConnectionType.Broadband => "B",
        _ => "T"
    };

    public async Task<(string OrderNumber, long SequenceNumber)> GetNextOrderNumberAsync(ConnectionType connectionType, CancellationToken cancellationToken = default)
    {
        var maxSequence = await db.Orders
            .Where(o => o.ConnectionType == connectionType)
            .MaxAsync(o => (long?)o.SequenceNumber, cancellationToken) ?? 0;

        var nextSequence = maxSequence + 1;
        return (PrefixFor(connectionType) + nextSequence.ToString("D10"), nextSequence);
    }

    public async Task<(string AccountNumber, long SerialNumber)> GetNextAccountNumberAsync(ConnectionType connectionType, int cityId, CancellationToken cancellationToken = default)
    {
        var city = await db.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cityId, cancellationToken)
            ?? throw new InvalidOperationException("City not found for account number generation.");

        var cityCode = city.Code.PadLeft(3, '0');

        var maxSerial = await db.Connections
            .Where(c => c.ConnectionType == connectionType && c.CityId == cityId)
            .MaxAsync(c => (long?)c.AccountSerialNumber, cancellationToken) ?? 0;

        var nextSerial = maxSerial + 1;
        return (PrefixFor(connectionType) + cityCode + nextSerial.ToString("D12"), nextSerial);
    }
}
