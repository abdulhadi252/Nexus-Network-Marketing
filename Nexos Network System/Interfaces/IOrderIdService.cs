using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Interfaces;

public interface IOrderIdService
{
    /// <summary>
    /// Generates the next sequential 11 character order number for a connection type,
    /// e.g. D0000000001 / B0000000002 / T0000000003.
    /// </summary>
    Task<(string OrderNumber, long SequenceNumber)> GetNextOrderNumberAsync(ConnectionType connectionType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the next 16 character account number: type prefix + 3 digit city code + 12 digit serial,
    /// e.g. B001000000000001.
    /// </summary>
    Task<(string AccountNumber, long SerialNumber)> GetNextAccountNumberAsync(ConnectionType connectionType, int cityId, CancellationToken cancellationToken = default);
}
