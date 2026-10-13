using System.ComponentModel.DataAnnotations;

namespace Nexos_Network_System.ViewModels;

public class CustomerFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "National ID")]
    public string? NationalId { get; set; }

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(30)]
    [Display(Name = "Primary Contact Number")]
    public string PrimaryContactNumber { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Alternate Contact Number")]
    public string? AlternateContactNumber { get; set; }

    [Required, StringLength(250)]
    [Display(Name = "Address Line 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "Address Line 2")]
    public string? AddressLine2 { get; set; }

    [StringLength(20)]
    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [Required, Display(Name = "City")]
    public int CityId { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
