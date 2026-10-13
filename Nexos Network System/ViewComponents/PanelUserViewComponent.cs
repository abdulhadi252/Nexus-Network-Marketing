using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.ViewComponents;

/// <summary>
/// Resolves the identity shown in the panel topbar chip.
///
/// The Identity cookie only carries the user name, so the friendly display name has to come from
/// the account record; falling back to the user name keeps the chip populated for accounts that
/// have no display name set.
/// </summary>
public class PanelUserViewComponent(ApplicationDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        // ViewComponent.User is typed as IPrincipal; the current-user helpers need the
        // ClaimsPrincipal the framework actually supplies.
        if (User is not ClaimsPrincipal principal)
        {
            return View(new PanelUserViewModel());
        }

        var email = principal.GetEmail();
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        var account = string.IsNullOrWhiteSpace(userId)
            ? null
            : await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        var displayName = !string.IsNullOrWhiteSpace(account?.FullName)
            ? account!.FullName!
            : email ?? "Account";

        return View(new PanelUserViewModel
        {
            DisplayName = displayName,
            Initial = string.IsNullOrWhiteSpace(displayName) ? "?" : displayName.Trim()[..1].ToUpperInvariant(),
            RoleLabel = ResolveRoleLabel(principal)
        });
    }

    private static string ResolveRoleLabel(ClaimsPrincipal principal) =>
        principal.IsInRole("Admin") ? "Administrator"
        : principal.IsInRole("Retail") ? "Retail Agent"
        : principal.IsInRole("Technical") ? "Technical Team"
        : principal.IsInRole("Accounts") ? "Accounts"
        : principal.IsInRole("Customer") ? "Customer"
        : "User";
}
