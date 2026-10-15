using Nexos_Network_System.Models;

namespace Nexos_Network_System.ViewModels;

public class CustomerDashboardViewModel
{
    /// <summary>Name used in the "Welcome back" greeting.</summary>
    public string GreetingName { get; set; } = "Customer";

    /// <summary>Human-readable range the headline numbers cover (the current calendar month).</summary>
    public string PeriodLabel { get; set; } = string.Empty;

    public IReadOnlyList<Connection> Connections { get; set; } = new List<Connection>();
    public Bill? CurrentBill { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int PendingOrders { get; set; }
    public decimal PaymentsMade { get; set; }
    public IReadOnlyList<Order> RecentOrders { get; set; } = new List<Order>();
    public IReadOnlyList<Payment> RecentPayments { get; set; } = new List<Payment>();
}
