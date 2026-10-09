namespace Nexos_Network_System.Models;

public class ConnectionPlanHistory
{
    public int Id { get; set; }
    public int ConnectionId { get; set; }
    public Connection Connection { get; set; } = null!;
    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public string? Reason { get; set; }
}
