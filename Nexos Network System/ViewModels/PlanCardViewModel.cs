using Nexos_Network_System.Models;

namespace Nexos_Network_System.ViewModels;

/// <summary>
/// Data for one plan tile rendered by the shared <c>_PlanCard</c> partial. The same card is
/// used on the order form (click to choose the plan) and on the current plan page (click to
/// read the plan's detail page).
/// </summary>
public class PlanCardViewModel
{
    public required Plan Plan { get; init; }

    /// <summary>Public plan detail page for this plan.</summary>
    public string? DetailsUrl { get; init; }

    /// <summary>True on the order form, where clicking the tile picks the plan in the dropdown.</summary>
    public bool Selectable { get; init; }

    /// <summary>Marks the plan that is currently chosen in the order form.</summary>
    public bool IsSelected { get; init; }

    /// <summary>Marks a plan the customer already holds a connection on.</summary>
    public bool IsCurrent { get; init; }
}
