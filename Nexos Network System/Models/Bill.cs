using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Models;

public class Bill
{
    public int Id { get; set; }
    public required string BillNumber { get; set; }
    public BillStatus Status { get; set; } = BillStatus.Unpaid;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int ConnectionId { get; set; }
    public Connection Connection { get; set; } = null!;
    public DateOnly BillingPeriodStart { get; set; }
    public DateOnly BillingPeriodEnd { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal PreviousPaidAmount { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal MonthlyRental { get; set; }
    public decimal EquipmentCharges { get; set; }
    public decimal ReplacementCharges { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SchemeDiscountAmount { get; set; }
    public decimal ServiceTaxRate { get; set; } = 0.1224m;
    public decimal ServiceTaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<BillItem> Items { get; set; } = new List<BillItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
