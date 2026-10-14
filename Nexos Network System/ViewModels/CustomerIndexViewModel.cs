using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;

namespace Nexos_Network_System.ViewModels;

/// <summary>
/// Backs the admin customer directory: the paged rows shown in the table plus the
/// summary tiles displayed above it and the values currently selected in the filters.
/// </summary>
public class CustomerIndexViewModel
{
    public PagedResult<Customer> Customers { get; init; } = new();

    /// <summary>Every customer on record, regardless of the active filters.</summary>
    public int TotalCustomers { get; init; }

    /// <summary>Connections currently in the Active state.</summary>
    public int ActiveConnections { get; init; }

    /// <summary>Distinct cities that customers have a registered address in.</summary>
    public int ServiceCities { get; init; }

    public string? Query { get; init; }

    /// <summary>True for active customers only, false for deactivated ones, null for both.</summary>
    public bool? IsActive { get; init; }

    public int? CityId { get; init; }

    public IReadOnlyList<City> Cities { get; init; } = Array.Empty<City>();

    /// <summary>1-based number of the first row on the current page, or 0 when the list is empty.</summary>
    public int FirstRowNumber => Customers.Items.Count == 0
        ? 0
        : ((Customers.Page - 1) * Customers.PageSize) + 1;

    /// <summary>1-based number of the last row on the current page.</summary>
    public int LastRowNumber => ((Customers.Page - 1) * Customers.PageSize) + Customers.Items.Count;
}
