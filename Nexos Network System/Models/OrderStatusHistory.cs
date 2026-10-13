using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Models;

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public int? ChangedByEmployeeId { get; set; }
    public Employee? ChangedByEmployee { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
}
