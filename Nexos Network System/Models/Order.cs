using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Models;

public class Order
{
    public int Id { get; set; }
    public required string OrderNumber { get; set; }
    public ConnectionType ConnectionType { get; set; }
    public long SequenceNumber { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int? RetailShopId { get; set; }
    public RetailShop? RetailShop { get; set; }
    public int? ProcessedByEmployeeId { get; set; }
    public Employee? ProcessedByEmployee { get; set; }
    public int? PlanId { get; set; }
    public Plan? Plan { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Remarks { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public FeasibilityReport? FeasibilityReport { get; set; }
    public Connection? Connection { get; set; }
}
