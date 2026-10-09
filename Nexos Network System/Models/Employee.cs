using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Models;

public class Employee
{
    public int Id { get; set; }
    public required string EmployeeNumber { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string ContactNumber { get; set; }
    public EmployeeRole Role { get; set; }
    public int? RetailShopId { get; set; }
    public RetailShop? RetailShop { get; set; }
    public DateTimeOffset HireDate { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Order> ProcessedOrders { get; set; } = new List<Order>();
    public ICollection<FeasibilityReport> FeasibilityReports { get; set; } = new List<FeasibilityReport>();
    public ICollection<Payment> RecordedPayments { get; set; } = new List<Payment>();
}
