using System.ComponentModel.DataAnnotations;

namespace Nexos_Network_System.ViewModels;

public class VendorFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(30)]
    [Display(Name = "Contact Number")]
    public string ContactNumber { get; set; } = string.Empty;

    [EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
