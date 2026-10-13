namespace Nexos_Network_System.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string UnitOfMeasure { get; set; }
    public decimal UnitCost { get; set; }
    public decimal? UnitSalePrice { get; set; }
    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public Stock? Stock { get; set; }
    public ICollection<Equipment> EquipmentItems { get; set; } = new List<Equipment>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
