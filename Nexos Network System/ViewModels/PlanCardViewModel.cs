using Nexos_Network_System.Models;

namespace Nexos_Network_System.ViewModels;

/// <summary>
/// Data for one plan tile rendered by the shared <c>_PlanCard</c> partial. The same card is
/// used on the order form and on the current plan page; the whole tile links to
/// <see cref="DetailsUrl"/>, where the customer can order the plan.
/// </summary>
public class PlanCardViewModel
{
    public required Plan Plan { get; init; }

    /// <summary>Public plan detail page for this plan; the tile links here.</summary>
    public string? DetailsUrl { get; init; }

    /// <summary>Marks a plan the customer already holds a connection on.</summary>
    public bool IsCurrent { get; init; }
}
