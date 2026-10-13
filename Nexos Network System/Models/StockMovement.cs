using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Models;

public class StockMovement
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public Stock Stock { get; set; } = null!;
    public StockMovementType MovementType { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityAfter { get; set; }
    public string? Reason { get; set; }
    public int? PerformedByEmployeeId { get; set; }
    public Employee? PerformedByEmployee { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
