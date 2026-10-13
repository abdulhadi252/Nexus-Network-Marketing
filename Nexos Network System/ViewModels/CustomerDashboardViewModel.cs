using Nexos_Network_System.Models;

namespace Nexos_Network_System.ViewModels;

public class CustomerDashboardViewModel
{
    public IReadOnlyList<Connection> Connections { get; set; } = new List<Connection>();
    public Bill? CurrentBill { get; set; }
    public decimal OutstandingAmount { get; set; }
    public IReadOnlyList<Order> RecentOrders { get; set; } = new List<Order>();
    public IReadOnlyList<Payment> RecentPayments { get; set; } = new List<Payment>();
}
