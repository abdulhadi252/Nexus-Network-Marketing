namespace Nexos_Network_System.Models;

/// <summary>
/// An in-panel message addressed to one customer. Accounts writes one when it sends a bill
/// notification, so the customer sees it in their own panel (bell + Notifications page) and pays
/// the bill from there.
/// </summary>
public class CustomerNotification
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>The bill this message is about, when it is about one.</summary>
    public int? BillId { get; set; }
    public Bill? Bill { get; set; }

    public required string Title { get; set; }
    public required string Message { get; set; }

    /// <summary>Amount the customer has to pay, shown as a chip on the customer's page.</summary>
    public decimal? Amount { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }

    public int? CreatedByEmployeeId { get; set; }
    public Employee? CreatedByEmployee { get; set; }
}
