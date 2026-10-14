using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class EmployeesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(string? q, EmployeeRole? role, int page = 1)
    {
        ViewData["PanelName"] = "Admin Panel";

        var baseQuery = db.Employees.Include(e => e.RetailShop).AsNoTracking().AsQueryable();

        // Summary tiles: counts from the unfiltered set so the tiles stay stable while paging.
        var totalEmployees = await baseQuery.CountAsync();
        var activeEmployees = await baseQuery.CountAsync(e => e.IsActive);
        var staffRoles = await baseQuery.Select(e => e.Role).Distinct().CountAsync();

        var query = baseQuery;

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(e =>
                e.EmployeeNumber.Contains(term) ||
                e.FirstName.Contains(term) ||
                e.LastName.Contains(term) ||
                e.Email.Contains(term) ||
                e.ContactNumber.Contains(term));
        }

        if (role.HasValue)
        {
            query = query.Where(e => e.Role == role.Value);
        }

        var employees = await PagedResult<Employee>.CreateAsync(query.OrderBy(e => e.EmployeeNumber), page);

        var model = new EmployeeIndexViewModel
        {
            Employees = employees,
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees,
            StaffRoles = staffRoles,
            Query = q,
            Role = role
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var employee = await db.Employees
            .Include(e => e.RetailShop)
            .Include(e => e.ProcessedOrders)
            .Include(e => e.FeasibilityReports)
            .Include(e => e.RecordedPayments)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null)
        {
            return NotFound();
        }

        return View(employee);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["PanelName"] = "Admin Panel";
        await LoadShopsAsync();
        LoadEmployeeRoles();
        return View(new EmployeeFormViewModel { EmployeeNumber = await NextEmployeeNumberAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";
        model.Id = 0;
        var email = model.Email.Trim();

        if (await db.Employees.AnyAsync(e => e.EmployeeNumber == model.EmployeeNumber.Trim()))
        {
            ModelState.AddModelError(nameof(model.EmployeeNumber), "This employee number is already in use.");
        }

        if (await db.Employees.AnyAsync(e => e.Email == email) ||
            await userManager.FindByEmailAsync(email) is not null ||
            await db.Customers.AnyAsync(c => c.Email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "This email address is already in use.");
        }

        if (model.Role == EmployeeRole.Retail &&
            (model.RetailShopId is null || !await db.RetailShops.AnyAsync(s => s.Id == model.RetailShopId && s.IsActive)))
        {
            ModelState.AddModelError(nameof(model.RetailShopId), "Select an active shop for the retail employee.");
        }

        if (!ModelState.IsValid)
        {
            await LoadShopsAsync();
            LoadEmployeeRoles();
            return View(model);
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = $"{model.FirstName.Trim()} {model.LastName.Trim()}",
            PhoneNumber = model.ContactNumber.Trim(),
            LockoutEnabled = true,
            LockoutEnd = model.IsActive ? null : DateTimeOffset.MaxValue
        };

        var createResult = await userManager.CreateAsync(user, model.InitialPassword!);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync();
            AddIdentityErrors(createResult);
            await LoadShopsAsync();
            LoadEmployeeRoles();
            return View(model);
        }

        var roleResult = await userManager.AddToRoleAsync(user, model.Role.ToString());
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            AddIdentityErrors(roleResult);
            await LoadShopsAsync();
            LoadEmployeeRoles();
            return View(model);
        }

        db.Employees.Add(new Employee
        {
            EmployeeNumber = model.EmployeeNumber.Trim(),
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = email,
            ContactNumber = model.ContactNumber.Trim(),
            Role = model.Role,
            RetailShopId = model.Role == EmployeeRole.Retail ? model.RetailShopId : null,
            HireDate = model.HireDate,
            IsActive = model.IsActive
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] = $"Employee '{model.FirstName} {model.LastName}' and their login account were created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null)
        {
            return NotFound();
        }

        await LoadShopsAsync();
        LoadEmployeeRoles(allowAdmin: employee.Role == EmployeeRole.Admin);
        return View(new EmployeeFormViewModel
        {
            Id = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            ContactNumber = employee.ContactNumber,
            Role = employee.Role,
            RetailShopId = employee.RetailShopId,
            HireDate = employee.HireDate,
            IsActive = employee.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null)
        {
            return NotFound();
        }

        model.Id = employee.Id;
        var updatedEmail = model.Email.Trim();
        if (await db.Employees.AnyAsync(e => e.EmployeeNumber == model.EmployeeNumber.Trim() && e.Id != id))
        {
            ModelState.AddModelError(nameof(model.EmployeeNumber), "This employee number is already in use.");
        }

        if (await db.Employees.AnyAsync(e => e.Email == updatedEmail && e.Id != id) ||
            await db.Customers.AnyAsync(c => c.Email == updatedEmail))
        {
            ModelState.AddModelError(nameof(model.Email), "This email address is already in use.");
        }

        if (model.Role == EmployeeRole.Admin && employee.Role != EmployeeRole.Admin)
        {
            ModelState.AddModelError(nameof(model.Role), "Admin accounts cannot be assigned through employee management.");
        }

        if (model.Role == EmployeeRole.Retail &&
            (model.RetailShopId is null || !await db.RetailShops.AnyAsync(s => s.Id == model.RetailShopId && s.IsActive)))
        {
            ModelState.AddModelError(nameof(model.RetailShopId), "Select an active shop for the retail employee.");
        }

        if (!ModelState.IsValid)
        {
            await LoadShopsAsync();
            LoadEmployeeRoles(allowAdmin: employee.Role == EmployeeRole.Admin);
            return View(model);
        }

        var linkedUser = await userManager.FindByEmailAsync(employee.Email);
        var accountAtUpdatedEmail = await userManager.FindByEmailAsync(updatedEmail);
        if (accountAtUpdatedEmail is not null && accountAtUpdatedEmail.Id != linkedUser?.Id)
        {
            ModelState.AddModelError(nameof(model.Email), "This email address is already in use.");
            await LoadShopsAsync();
            LoadEmployeeRoles(allowAdmin: employee.Role == EmployeeRole.Admin);
            return View(model);
        }

        if (linkedUser is not null)
        {
            var linkedRoles = await userManager.GetRolesAsync(linkedUser);
            if (linkedUser.CustomerId is not null || linkedRoles.Contains("Customer", StringComparer.Ordinal))
            {
                ModelState.AddModelError(string.Empty, "A customer account cannot be reassigned as an employee account.");
                await LoadShopsAsync();
                LoadEmployeeRoles(allowAdmin: employee.Role == EmployeeRole.Admin);
                return View(model);
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        if (linkedUser is not null)
        {
            linkedUser.Email = updatedEmail;
            linkedUser.UserName = updatedEmail;
            linkedUser.EmailConfirmed = true;
            linkedUser.FullName = $"{model.FirstName.Trim()} {model.LastName.Trim()}";
            linkedUser.PhoneNumber = model.ContactNumber.Trim();
            linkedUser.LockoutEnabled = true;
            linkedUser.LockoutEnd = model.IsActive ? null : DateTimeOffset.MaxValue;

            var updateResult = await userManager.UpdateAsync(linkedUser);
            if (!updateResult.Succeeded)
            {
                await transaction.RollbackAsync();
                AddIdentityErrors(updateResult);
                await LoadShopsAsync();
                LoadEmployeeRoles(allowAdmin: employee.Role == EmployeeRole.Admin);
                return View(model);
            }

            var targetRole = model.Role.ToString();
            var currentRoles = await userManager.GetRolesAsync(linkedUser);
            var employeeRoles = new[] { "Admin", "Retail", "Technical", "Accounts" };
            foreach (var currentRole in currentRoles.Where(role => employeeRoles.Contains(role) && role != targetRole))
            {
                var removeResult = await userManager.RemoveFromRoleAsync(linkedUser, currentRole);
                if (!removeResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    AddIdentityErrors(removeResult);
                    await LoadShopsAsync();
                    LoadEmployeeRoles(allowAdmin: employee.Role == EmployeeRole.Admin);
                    return View(model);
                }
            }

            if (!currentRoles.Contains(targetRole, StringComparer.Ordinal))
            {
                var addResult = await userManager.AddToRoleAsync(linkedUser, targetRole);
                if (!addResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    AddIdentityErrors(addResult);
                    await LoadShopsAsync();
                    LoadEmployeeRoles(allowAdmin: employee.Role == EmployeeRole.Admin);
                    return View(model);
                }
            }

            var stampResult = await userManager.UpdateSecurityStampAsync(linkedUser);
            if (!stampResult.Succeeded)
            {
                await transaction.RollbackAsync();
                AddIdentityErrors(stampResult);
                await LoadShopsAsync();
                LoadEmployeeRoles(allowAdmin: employee.Role == EmployeeRole.Admin);
                return View(model);
            }
        }

        employee.EmployeeNumber = model.EmployeeNumber.Trim();
        employee.FirstName = model.FirstName.Trim();
        employee.LastName = model.LastName.Trim();
        employee.Email = updatedEmail;
        employee.ContactNumber = model.ContactNumber.Trim();
        employee.Role = model.Role;
        employee.RetailShopId = model.Role == EmployeeRole.Retail ? model.RetailShopId : null;
        employee.HireDate = model.HireDate;
        employee.IsActive = model.IsActive;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] = "Employee details were updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["PanelName"] = "Admin Panel";

        var employee = await db.Employees.Include(e => e.RetailShop).AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null)
        {
            return NotFound();
        }

        return View(employee);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null)
        {
            return NotFound();
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        employee.IsActive = false;
        var linkedUser = await userManager.FindByEmailAsync(employee.Email);
        if (linkedUser is not null)
        {
            linkedUser.LockoutEnabled = true;
            linkedUser.LockoutEnd = DateTimeOffset.MaxValue;
            var lockResult = await userManager.UpdateAsync(linkedUser);
            if (!lockResult.Succeeded)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = string.Join(" ", lockResult.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(Index));
            }

            var stampResult = await userManager.UpdateSecurityStampAsync(linkedUser);
            if (!stampResult.Succeeded)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = string.Join(" ", stampResult.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(Index));
            }
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] = "Employee was deactivated. Historical orders, feasibility reports and payments remain intact.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadShopsAsync()
    {
        ViewBag.RetailShops = new SelectList(
            await db.RetailShops.AsNoTracking().OrderBy(s => s.Name).ToListAsync(),
            "Id", "Name");
    }

    private void LoadEmployeeRoles(bool allowAdmin = false)
    {
        ViewBag.EmployeeRoles = new SelectList(
            Enum.GetValues<EmployeeRole>()
                .Where(role => allowAdmin || role != EmployeeRole.Admin)
                .Select(role => new { Value = (int)role, Name = role.ToString() }),
            "Value",
            "Name");
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private async Task<string> NextEmployeeNumberAsync()
    {
        var count = await db.Employees.CountAsync();
        return $"EMP-{(count + 1):D4}";
    }
}
