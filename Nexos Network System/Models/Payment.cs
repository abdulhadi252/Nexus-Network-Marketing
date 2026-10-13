using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Models;

public class Payment
{
    public int Id { get; set; }
    public required string ReceiptNumber { get; set; }
    public int BillId { get; set; }
    public Bill Bill { get; set; } = null!;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int ConnectionId { get; set; }
    public Connection Connection { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTimeOffset PaidAt { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Recorded;
    public string? PaymentReference { get; set; }
    public int? RecordedByEmployeeId { get; set; }
    public Employee? RecordedByEmployee { get; set; }
    public string? Notes { get; set; }
}
