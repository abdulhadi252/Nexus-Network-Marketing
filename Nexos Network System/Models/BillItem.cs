namespace Nexos_Network_System.Models;

public class BillItem
{
    public int Id { get; set; }
    public int BillId { get; set; }
    public Bill Bill { get; set; } = null!;
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsTaxable { get; set; }
}
