using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

/// <summary>
/// Backs the admin connections list: the paged rows shown in the table plus the four
/// summary tiles displayed above it and the values currently selected in the filters.
/// </summary>
public class ConnectionIndexViewModel
{
    public PagedResult<Connection> Connections { get; init; } = new();

    /// <summary>Every connection on record, regardless of the active filters.</summary>
    public int TotalConnections { get; init; }

    /// <summary>Connections currently in the Active state.</summary>
    public int ActiveConnections { get; init; }

    /// <summary>Connections currently in the Pending state.</summary>
    public int PendingConnections { get; init; }

    /// <summary>Connections in any suspended / inactive state.</summary>
    public int SuspendedConnections { get; init; }

    public string? Query { get; init; }

    public ConnectionStatus? Status { get; init; }

    public ConnectionType? ConnectionType { get; init; }

    /// <summary>1-based number of the first row on the current page, or 0 when the list is empty.</summary>
    public int FirstRowNumber => Connections.Items.Count == 0
        ? 0
        : ((Connections.Page - 1) * Connections.PageSize) + 1;

    /// <summary>1-based number of the last row on the current page.</summary>
    public int LastRowNumber => ((Connections.Page - 1) * Connections.PageSize) + Connections.Items.Count;

    /// <summary>Distinct connection types, for the "Connection type" filter.</summary>
    public IReadOnlyList<ConnectionType> ConnectionTypes { get; init; } = Enum.GetValues<ConnectionType>().Cast<ConnectionType>().ToList().AsReadOnly();
}
