using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Models;

public class Connection
{
    public int Id { get; set; }
    public required string AccountNumber { get; set; }
    public ConnectionType ConnectionType { get; set; }
    public long AccountSerialNumber { get; set; }
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int CityId { get; set; }
    public City City { get; set; } = null!;
    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public DateTimeOffset? InstalledAt { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }
    public string? InstallationNotes { get; set; }
    public ICollection<Equipment> EquipmentItems { get; set; } = new List<Equipment>();
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<ConnectionPlanHistory> PlanHistory { get; set; } = new List<ConnectionPlanHistory>();
}
