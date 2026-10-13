namespace Nexos_Network_System.Models;

public class Customer
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? NationalId { get; set; }
    public required string Email { get; set; }
    public required string PrimaryContactNumber { get; set; }
    public string? AlternateContactNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;
    public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Connection> Connections { get; set; } = new List<Connection>();
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Feedback> FeedbackEntries { get; set; } = new List<Feedback>();
}
