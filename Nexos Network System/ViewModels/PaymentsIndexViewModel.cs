using System.Collections.ObjectModel;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

public class PaymentsIndexViewModel
{
    public IReadOnlyList<Payment> Payments { get; set; } = Array.Empty<Payment>();

    // Filter inputs echoed back to the view so the form and pager stay in sync.
    public string? Q { get; set; }
    public PaymentMethod? Method { get; set; }
    public PaymentStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    // Paging.
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize)) : 1;
    public bool HasFilters => !string.IsNullOrWhiteSpace(Q)
        || Method.HasValue
        || Status.HasValue
        || From.HasValue
        || To.HasValue;

    // Summary cards.
    public int AllCount { get; set; }
    public decimal TotalCollected { get; set; }
    public int SuccessCount { get; set; }
    public double SuccessRate { get; set; }
    public int PendingCount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal RefundedAmount { get; set; }

    // Paging window helpers.
    public int ShowingFrom => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int ShowingTo => Math.Min(Page * PageSize, TotalCount);
}
