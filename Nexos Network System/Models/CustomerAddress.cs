namespace Nexos_Network_System.Models;

public class CustomerAddress
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int CityId { get; set; }
    public City City { get; set; } = null!;
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? PostalCode { get; set; }
    public bool IsPrimary { get; set; }
}
