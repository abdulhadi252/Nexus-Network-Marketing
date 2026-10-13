using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

public class OrderFilterViewModel
{
    public string? Query { get; set; }
    public OrderStatus? Status { get; set; }
    public ConnectionType? ConnectionType { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int Page { get; set; } = 1;
}
