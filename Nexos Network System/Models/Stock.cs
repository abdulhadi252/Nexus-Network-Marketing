namespace Nexos_Network_System.Models;

public class Stock
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int ReorderLevel { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
}
