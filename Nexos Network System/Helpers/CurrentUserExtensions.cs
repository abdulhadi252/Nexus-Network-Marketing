using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Helpers;

public static class CurrentUserExtensions
{
    public static string? GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name;

    /// <summary>
    /// Maps the signed in user's panel role to its area name
    /// (Admin, Retail, Technical, Accounts, Customer) or null when no panel role matches.
    /// </summary>
    public static string? GetPanelAreaName(this ClaimsPrincipal user) =>
        user.IsInRole("Admin") ? "Admin"
        : user.IsInRole("Retail") ? "Retail"
        : user.IsInRole("Technical") ? "Technical"
        : user.IsInRole("Accounts") ? "Accounts"
        : user.IsInRole("Customer") ? "Customer"
        : null;

    /// <summary>
    /// First name used by the panel dashboard greeting. Prefers the account's display name
    /// and falls back to the e-mail local part, so the greeting is never blank.
    /// </summary>
    public static async Task<string> GetGreetingNameAsync(this ClaimsPrincipal user, ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var fullName = string.IsNullOrWhiteSpace(userId)
            ? null
            : await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);

        var source = string.IsNullOrWhiteSpace(fullName) ? user.GetEmail() : fullName;
        if (string.IsNullOrWhiteSpace(source))
        {
            return "there";
        }

        return source.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    }

    /// <summary>Resolves the Employee record that belongs to the signed in user (matched on email).</summary>
    public static async Task<Employee?> GetEmployeeAsync(this ClaimsPrincipal user, ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var email = user.GetEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await db.Employees
            .AsNoTracking()
            .Include(e => e.RetailShop)
            .FirstOrDefaultAsync(e => e.Email == email, cancellationToken);
    }

    /// <summary>Resolves the active retail shop assigned to the signed-in retail employee.</summary>
    public static async Task<int?> GetRetailShopIdAsync(this ClaimsPrincipal user, ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var email = user.GetEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await db.Employees
            .AsNoTracking()
            .Where(e => e.Email == email && e.IsActive && e.Role == EmployeeRole.Retail &&
                        e.RetailShopId.HasValue && e.RetailShop!.IsActive)
            .Select(e => e.RetailShopId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Resolves the Customer id linked to the signed in Identity user.</summary>
    public static async Task<int?> GetCustomerIdAsync(this ClaimsPrincipal user, ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.CustomerId.HasValue && u.Customer != null && u.Customer.IsActive)
            .Select(u => u.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Resolves the Customer record linked to the signed in Identity user.</summary>
    public static async Task<Customer?> GetCustomerAsync(this ClaimsPrincipal user, ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var customerId = await user.GetCustomerIdAsync(db, cancellationToken);
        if (customerId is null)
        {
            return null;
        }

        return await db.Customers
            .Include(c => c.Addresses).ThenInclude(a => a.City)
            .FirstOrDefaultAsync(c => c.Id == customerId.Value, cancellationToken);
    }
}
