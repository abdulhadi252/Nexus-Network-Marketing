using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

public class SearchViewModel
{
    /// <summary>
    /// Free-text term used by the panel topbar search: matched against order numbers, account
    /// numbers, customer names and contact numbers at once.
    /// </summary>
    public string? Query { get; set; }

    public string? OrderNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? ContactNumber { get; set; }
    public ConnectionType? ConnectionType { get; set; }
    public OrderStatus? OrderStatus { get; set; }
    public ConnectionStatus? ConnectionStatus { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }

    public bool HasSearched { get; set; }

    public IReadOnlyList<Order> Orders { get; set; } = new List<Order>();
    public IReadOnlyList<Connection> Connections { get; set; } = new List<Connection>();
    public IReadOnlyList<Customer> Customers { get; set; } = new List<Customer>();
}
