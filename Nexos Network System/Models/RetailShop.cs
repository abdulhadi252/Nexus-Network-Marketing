namespace Nexos_Network_System.Models;

public class RetailShop
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string ContactNumber { get; set; }
    public int CityId { get; set; }
    public City City { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
