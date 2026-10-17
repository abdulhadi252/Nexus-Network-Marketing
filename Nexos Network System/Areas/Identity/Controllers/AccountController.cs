using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Controllers;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.ViewModels;

// "Customer" resolves to the Areas.Customer namespace inside area controller namespaces,
// so the model needs an explicit alias.
using CustomerModel = Nexos_Network_System.Models.Customer;

namespace Nexos_Network_System.Areas.Identity.Controllers;

[Area("Identity")]
public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db) : AccountProfileControllerBase(userManager, signInManager, db)
{
    protected override string PanelName => "Account";
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPanelHome(User.GetPanelAreaName());
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await UserManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        var roles = await UserManager.GetRolesAsync(user);
        if (roles.Any(role => role is "Admin" or "Retail" or "Technical" or "Accounts") &&
            !await Db.Employees.AnyAsync(employee =>
                employee.Email == user.Email && employee.IsActive &&
                (employee.Role.ToString() != "Retail" ||
                 employee.RetailShopId.HasValue && employee.RetailShop!.IsActive)))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (roles.Contains("Customer", StringComparer.Ordinal) &&
            (user.CustomerId is null ||
             !await Db.Customers.AnyAsync(customer => customer.Id == user.CustomerId && customer.IsActive)))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        var result = await SignInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return RedirectToLocal(model.ReturnUrl) ?? RedirectToPanelHome(roles.GetPanelAreaName());
        }

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "This account is temporarily locked after too many failed attempts. Please try again later."
            : "Invalid email or password.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await SignInManager.SignOutAsync();
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await UserManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Send every user to their own panel profile (/Admin/Profile, /Customer/Profile, ...).
        var panelArea = User.GetPanelAreaName();
        if (panelArea is not null)
        {
            return RedirectToAction("Index", "Profile", new { area = panelArea });
        }

        // Fallback for accounts without a panel role: read-only account summary.
        var roles = await UserManager.GetRolesAsync(user);
        var model = new ProfileViewModel
        {
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Roles = string.Join(", ", roles)
        };

        ViewData["Title"] = "My Profile";
        return View("~/Views/Shared/Account/Profile.cshtml", model);
    }

    // ------------------------------------------------------------------
    // Public customer self-registration (role Customer, linked to a new Customer record)
    // ------------------------------------------------------------------

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPanelHome(User.GetPanelAreaName());
        }

        await LoadRegistrationCitiesAsync();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPanelHome(User.GetPanelAreaName());
        }

        if (!ModelState.IsValid)
        {
            await LoadRegistrationCitiesAsync();
            return View(model);
        }

        var email = model.Email.Trim();

        // Duplicate emails are rejected against both Identity accounts and customer records.
        if (await UserManager.FindByEmailAsync(email) is not null ||
            await Db.Customers.AnyAsync(c => c.Email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "This email address is already registered.");
            await LoadRegistrationCitiesAsync();
            return View(model);
        }

        if (!await Db.Cities.AnyAsync(c => c.Id == model.CityId!.Value))
        {
            ModelState.AddModelError(nameof(model.CityId), "The selected city is not available.");
            await LoadRegistrationCitiesAsync();
            return View(model);
        }

        var firstName = model.FirstName.Trim();
        var lastName = model.LastName.Trim();

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var customer = new CustomerModel
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PrimaryContactNumber = model.ContactNumber.Trim()
        };
        customer.Addresses.Add(new CustomerAddress
        {
            AddressLine1 = model.AddressLine1.Trim(),
            AddressLine2 = model.AddressLine2?.Trim(),
            PostalCode = model.PostalCode?.Trim(),
            CityId = model.CityId!.Value,
            IsPrimary = true
        });
        Db.Customers.Add(customer);
        await Db.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = $"{firstName} {lastName}".Trim(),
            CustomerId = customer.Id
        };

        var result = await UserManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            await LoadRegistrationCitiesAsync();
            return View(model);
        }

        var roleResult = await UserManager.AddToRoleAsync(user, "Customer");
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            await LoadRegistrationCitiesAsync();
            return View(model);
        }

        await transaction.CommitAsync();

        await SignInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
    }

    private async Task LoadRegistrationCitiesAsync()
    {
        ViewBag.Cities = new SelectList(
            await Db.Cities.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
            nameof(City.Id), nameof(City.Name));
    }

    private IActionResult? RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return null;
    }

    /// <summary>
    /// Sends a signed-in user straight to their own panel dashboard (/Admin/Dashboard,
    /// /Customer/Dashboard, ...) so opening a panel never lands on the profile page.
    /// Accounts without a panel role fall back to the read-only account profile summary.
    /// </summary>
    private IActionResult RedirectToPanelHome(string? panelArea) =>
        panelArea is null
            ? RedirectToAction(nameof(Profile))
            : RedirectToAction("Index", "Dashboard", new { area = panelArea });
}
