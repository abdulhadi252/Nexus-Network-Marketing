using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Models;

public class Equipment
{
    public int Id { get; set; }
    public required string AssetTag { get; set; }
    public string? SerialNumber { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public int? ConnectionId { get; set; }
    public Connection? Connection { get; set; }
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Available;
    public decimal? ReplacementCharge { get; set; }
    public DateTimeOffset? PurchasedAt { get; set; }
    public DateTimeOffset? AssignedAt { get; set; }
    public string? Notes { get; set; }
}
