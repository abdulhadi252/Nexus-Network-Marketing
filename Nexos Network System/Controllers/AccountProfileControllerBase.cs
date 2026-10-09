using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Controllers;

/// <summary>
/// Shared implementation behind the per-area profile routes
/// (/Admin/Profile, /Retail/Profile, /Technical/Profile, /Accounts/Profile, /Customer/Profile).
///
/// Security rules enforced here:
/// - The current user is ALWAYS resolved from the authenticated Identity context
///   (UserManager.GetUserAsync). No id, role or ownership value is ever read from the
///   route, query string, hidden fields or the request payload.
/// - Users can only edit fields appropriate for their own account. Roles, employee
///   numbers, shop assignment, account numbers and security fields are never editable
///   through these forms (that stays with the Admin workflows).
/// - Password changes go through ASP.NET Core Identity (ChangePasswordAsync); no custom
///   hashing, no plain-text storage, nothing password-related is logged or displayed.
/// </summary>
[Authorize]
public abstract class AccountProfileControllerBase(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext db) : Controller
{
    protected const string ProfileViewPath = "~/Views/Shared/Account/Profile.cshtml";
    protected const string EditProfileViewPath = "~/Views/Shared/Account/EditProfile.cshtml";
    protected const string ChangePasswordViewPath = "~/Views/Shared/Account/ChangePassword.cshtml";

    protected readonly UserManager<ApplicationUser> UserManager = userManager;
    protected readonly SignInManager<ApplicationUser> SignInManager = signInManager;
    protected readonly ApplicationDbContext Db = db;

    /// <summary>Panel title shown in the panel layout (e.g. "Admin Panel").</summary>
    protected abstract string PanelName { get; }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewData["PanelName"] = PanelName;
        base.OnActionExecuting(context);
    }

    // ------------------------------------------------------------------
    // My Profile
    // ------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await UserManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToLogin();
        }

        var model = await BuildProfileViewModelAsync(user, cancellationToken);
        ViewData["Title"] = "My Profile";
        return View(ProfileViewPath, model);
    }

    // ------------------------------------------------------------------
    // Edit Profile
    // ------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Edit(CancellationToken cancellationToken)
    {
        var user = await UserManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToLogin();
        }

        var model = await BuildEditViewModelAsync(user, cancellationToken);
        ViewData["Title"] = "Edit Profile";
        return View(EditProfileViewPath, model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel model, CancellationToken cancellationToken)
    {
        var user = await UserManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToLogin();
        }

        var isCustomer = user.CustomerId.HasValue;

        // Server-side validation: an address needs both a street line and a city.
        if (isCustomer)
        {
            var hasLine1 = !string.IsNullOrWhiteSpace(model.AddressLine1);
            if (hasLine1 && !model.CityId.HasValue)
            {
                ModelState.AddModelError(nameof(model.CityId), "Select a city for the address.");
            }
            else if (model.CityId.HasValue && !hasLine1)
            {
                ModelState.AddModelError(nameof(model.AddressLine1), "Address line 1 is required when a city is selected.");
            }
            else if (model.CityId.HasValue && !await Db.Cities.AnyAsync(c => c.Id == model.CityId.Value, cancellationToken))
            {
                ModelState.AddModelError(nameof(model.CityId), "The selected city is not available.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.IsCustomer = isCustomer;
            model.Email = user.Email;
            await LoadCitiesAsync(model, cancellationToken);
            return View(EditProfileViewPath, model);
        }

        var firstName = model.FirstName.Trim();
        var lastName = model.LastName.Trim();
        var contactNumber = model.ContactNumber.Trim();

        // 1) Identity account fields (display name + phone) through the UserManager API.
        user.FullName = $"{firstName} {lastName}".Trim();
        user.PhoneNumber = contactNumber;
        var identityResult = await UserManager.UpdateAsync(user);
        if (!identityResult.Succeeded)
        {
            foreach (var error in identityResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.IsCustomer = isCustomer;
            model.Email = user.Email;
            await LoadCitiesAsync(model, cancellationToken);
            return View(EditProfileViewPath, model);
        }

        // 2) The linked business record. Employees never gain role / employee-number /
        //    shop-assignment changes here, customers never touch account, billing or
        //    connection data - those fields are simply absent from this flow.
        if (isCustomer)
        {
            var customer = await Db.Customers
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == user.CustomerId!.Value, cancellationToken);

            if (customer is not null)
            {
                customer.FirstName = firstName;
                customer.LastName = lastName;
                customer.PrimaryContactNumber = contactNumber;
                customer.AlternateContactNumber = model.AlternateContactNumber?.Trim();

                if (!string.IsNullOrWhiteSpace(model.AddressLine1) && model.CityId.HasValue)
                {
                    var address = customer.Addresses.OrderByDescending(a => a.IsPrimary).FirstOrDefault();
                    if (address is null)
                    {
                        address = new CustomerAddress { CustomerId = customer.Id, AddressLine1 = string.Empty, IsPrimary = true };
                        customer.Addresses.Add(address);
                    }

                    address.AddressLine1 = model.AddressLine1.Trim();
                    address.AddressLine2 = model.AddressLine2?.Trim();
                    address.CityId = model.CityId.Value;
                    address.PostalCode = model.PostalCode?.Trim();
                }
            }
        }
        else
        {
            var employee = string.IsNullOrWhiteSpace(user.Email)
                ? null
                : await Db.Employees.FirstOrDefaultAsync(e => e.Email == user.Email, cancellationToken);

            if (employee is not null)
            {
                employee.FirstName = firstName;
                employee.LastName = lastName;
                employee.ContactNumber = contactNumber;
            }
        }

        await Db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Your profile was updated.";
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------
    // Change Password (ASP.NET Core Identity handles verification + hashing)
    // ------------------------------------------------------------------

    [HttpGet]
    public IActionResult ChangePassword()
    {
        ViewData["Title"] = "Change Password";
        return View(ChangePasswordViewPath, new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(ChangePasswordViewPath, model);
        }

        var user = await UserManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToLogin();
        }

        var result = await UserManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            // Identity returns wrong-current-password and policy violations here.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(ChangePasswordViewPath, model);
        }

        // Re-issue the cookie because changing the password also changes the security stamp.
        await SignInManager.RefreshSignInAsync(user);

        TempData["Success"] = "Your password has been changed successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private async Task<ProfileViewModel> BuildProfileViewModelAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await UserManager.GetRolesAsync(user);
        var model = new ProfileViewModel
        {
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Roles = string.Join(", ", roles)
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            model.Employee = await Db.Employees
                .AsNoTracking()
                .Include(e => e.RetailShop)
                .FirstOrDefaultAsync(e => e.Email == user.Email, cancellationToken);
        }

        if (user.CustomerId.HasValue)
        {
            model.Customer = await Db.Customers
                .AsNoTracking()
                .Include(c => c.Addresses).ThenInclude(a => a.City)
                .FirstOrDefaultAsync(c => c.Id == user.CustomerId.Value, cancellationToken);

            if (model.Customer is not null)
            {
                model.CustomerConnections = await Db.Connections
                    .AsNoTracking()
                    .Include(c => c.Plan)
                    .Include(c => c.City)
                    .Where(c => c.CustomerId == model.Customer.Id)
                    .OrderByDescending(c => c.Id)
                    .ToListAsync(cancellationToken);
            }
        }

        return model;
    }

    private async Task<ProfileEditViewModel> BuildEditViewModelAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var model = new ProfileEditViewModel
        {
            Email = user.Email,
            IsCustomer = user.CustomerId.HasValue,
            ContactNumber = user.PhoneNumber ?? string.Empty
        };

        if (model.IsCustomer)
        {
            var customer = await Db.Customers
                .AsNoTracking()
                .Include(c => c.Addresses).ThenInclude(a => a.City)
                .FirstOrDefaultAsync(c => c.Id == user.CustomerId!.Value, cancellationToken);

            if (customer is not null)
            {
                model.FirstName = customer.FirstName;
                model.LastName = customer.LastName;
                model.ContactNumber = customer.PrimaryContactNumber;
                model.AlternateContactNumber = customer.AlternateContactNumber;

                var address = customer.Addresses.OrderByDescending(a => a.IsPrimary).FirstOrDefault();
                model.AddressLine1 = address?.AddressLine1;
                model.AddressLine2 = address?.AddressLine2;
                model.PostalCode = address?.PostalCode;
                model.CityId = address?.CityId;
            }
        }
        else
        {
            var employee = string.IsNullOrWhiteSpace(user.Email)
                ? null
                : await Db.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Email == user.Email, cancellationToken);

            if (employee is not null)
            {
                model.FirstName = employee.FirstName;
                model.LastName = employee.LastName;
                model.ContactNumber = employee.ContactNumber;
            }
            else
            {
                // No linked employee/customer record: allow editing the Identity display name only.
                var parts = (user.FullName ?? string.Empty).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                model.FirstName = parts.Length > 0 ? parts[0] : string.Empty;
                model.LastName = parts.Length > 1 ? parts[1] : string.Empty;
            }
        }

        await LoadCitiesAsync(model, cancellationToken);
        return model;
    }

    private async Task LoadCitiesAsync(ProfileEditViewModel model, CancellationToken cancellationToken)
    {
        model.Cities = new SelectList(
            await Db.Cities.AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken),
            nameof(City.Id), nameof(City.Name));
    }

    private IActionResult RedirectToLogin() =>
        RedirectToAction("Login", "Account", new { area = "Identity" });
}
