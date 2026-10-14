using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

/// <summary>
/// Backs the admin employees list: the paged rows shown in the table plus the three
/// summary tiles displayed above it and the values currently selected in the filters.
/// </summary>
public class EmployeeIndexViewModel
{
    public PagedResult<Employee> Employees { get; init; } = new();

    /// <summary>Every employee on record, regardless of the active filters.</summary>
    public int TotalEmployees { get; init; }

    /// <summary>Employees currently marked active.</summary>
    public int ActiveEmployees { get; init; }

    /// <summary>Distinct staff roles represented across the employee records.</summary>
    public int StaffRoles { get; init; }

    public string? Query { get; init; }

    public EmployeeRole? Role { get; init; }

    /// <summary>Distinct roles, for the "All roles" filter dropdown.</summary>
    public IReadOnlyList<EmployeeRole> Roles { get; init; } =
        Enum.GetValues<EmployeeRole>().Cast<EmployeeRole>().ToList().AsReadOnly();

    /// <summary>1-based number of the first row on the current page, or 0 when the list is empty.</summary>
    public int FirstRowNumber => Employees.Items.Count == 0
        ? 0
        : ((Employees.Page - 1) * Employees.PageSize) + 1;

    /// <summary>1-based number of the last row on the current page.</summary>
    public int LastRowNumber => ((Employees.Page - 1) * Employees.PageSize) + Employees.Items.Count;

    /// <summary>Human-readable role label for the filter dropdown.</summary>
    public static string RoleLabel(EmployeeRole role) => role switch
    {
        EmployeeRole.Admin => "Admin",
        EmployeeRole.Accounts => "Accounts Staff",
        EmployeeRole.Technical => "Technical Staff",
        EmployeeRole.Retail => "Retail Staff",
        _ => role.ToString()
    };
}
