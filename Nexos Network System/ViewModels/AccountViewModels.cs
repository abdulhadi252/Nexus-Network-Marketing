using System.ComponentModel.DataAnnotations;

namespace Nexos_Network_System.ViewModels;

/// <summary>
/// Edit form for the signed in user's own profile. Contains no user/employee/customer
/// identifiers: the server always resolves the record from the authenticated Identity
/// context, so ids can never be tampered with through the form payload.
/// </summary>
public class ProfileEditViewModel
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact number is required.")]
    [StringLength(30)]
    [RegularExpression(@"^[0-9+\-\s()]{7,30}$", ErrorMessage = "Enter a valid contact number (digits, spaces, + - ( ) only).")]
    [Display(Name = "Contact Number")]
    public string ContactNumber { get; set; } = string.Empty;

    // --- Customer-only fields (ignored for employee accounts) ---

    [StringLength(30)]
    [RegularExpression(@"^[0-9+\-\s()]{7,30}$", ErrorMessage = "Enter a valid contact number (digits, spaces, + - ( ) only).")]
    [Display(Name = "Alternate Contact Number")]
    public string? AlternateContactNumber { get; set; }

    [StringLength(250)]
    [Display(Name = "Address Line 1")]
    public string? AddressLine1 { get; set; }

    [StringLength(250)]
    [Display(Name = "Address Line 2")]
    public string? AddressLine2 { get; set; }

    [StringLength(20)]
    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [Display(Name = "City")]
    public int? CityId { get; set; }

    // --- Server-populated display data (not posted by the client) ---

    public string? Email { get; set; }

    public bool IsCustomer { get; set; }

    public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Cities { get; set; }
        = Enumerable.Empty<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
}

/// <summary>
/// Change-password form. Verification, hashing and the password policy are handled
/// entirely by ASP.NET Core Identity (UserManager.ChangePasswordAsync).
/// </summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The new password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the new password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation do not match.")]
    [Display(Name = "Confirm New Password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Public customer self-registration. Creates a Customer record with a primary address
/// plus the linked Identity login (role Customer) inside a single transaction.
/// </summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact number is required.")]
    [StringLength(30)]
    [RegularExpression(@"^[0-9+\-\s()]{7,30}$", ErrorMessage = "Enter a valid contact number (digits, spaces, + - ( ) only).")]
    [Display(Name = "Contact Number")]
    public string ContactNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address line 1 is required.")]
    [StringLength(250)]
    [Display(Name = "Address Line 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "Address Line 2")]
    public string? AddressLine2 { get; set; }

    [Required(ErrorMessage = "Please select your city.")]
    [Display(Name = "City")]
    public int? CityId { get; set; }

    [StringLength(20)]
    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
