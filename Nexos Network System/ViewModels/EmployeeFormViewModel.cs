using System.ComponentModel.DataAnnotations;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.ViewModels;

public class EmployeeFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    [Display(Name = "Employee Number")]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(30)]
    [Display(Name = "Contact Number")]
    public string ContactNumber { get; set; } = string.Empty;

    [Display(Name = "Role")]
    public EmployeeRole Role { get; set; } = EmployeeRole.Retail;

    [Display(Name = "Retail Shop")]
    public int? RetailShopId { get; set; }

    [Display(Name = "Hire Date")]
    [DataType(DataType.Date)]
    public DateTimeOffset HireDate { get; set; } = DateTimeOffset.UtcNow;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The initial password must be at least 8 characters.")]
    [Display(Name = "Initial password")]
    public string? InitialPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(InitialPassword), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm initial password")]
    public string? ConfirmInitialPassword { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Id == 0)
        {
            if (string.IsNullOrWhiteSpace(InitialPassword))
            {
                yield return new ValidationResult("An initial password is required.", [nameof(InitialPassword)]);
            }

            if (string.IsNullOrWhiteSpace(ConfirmInitialPassword))
            {
                yield return new ValidationResult("Confirm the initial password.", [nameof(ConfirmInitialPassword)]);
            }

            if (Role == EmployeeRole.Admin)
            {
                yield return new ValidationResult("Admin accounts cannot be created through employee management.", [nameof(Role)]);
            }
        }

        if (!Enum.IsDefined(Role))
        {
            yield return new ValidationResult("Choose a valid employee role.", [nameof(Role)]);
        }

        if (Role == EmployeeRole.Retail && RetailShopId is null)
        {
            yield return new ValidationResult("Assign retail employees to a shop.", [nameof(RetailShopId)]);
        }
    }
}
