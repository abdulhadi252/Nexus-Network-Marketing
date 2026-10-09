namespace Nexos_Network_System.Models;

public class City
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<RetailShop> RetailShops { get; set; } = new List<RetailShop>();
    public ICollection<CustomerAddress> CustomerAddresses { get; set; } = new List<CustomerAddress>();
    public ICollection<Connection> Connections { get; set; } = new List<Connection>();
}
