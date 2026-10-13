namespace Nexos_Network_System.Models;

public class Vendor
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ContactNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Equipment> EquipmentItems { get; set; } = new List<Equipment>();
}
