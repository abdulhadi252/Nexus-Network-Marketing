using System.ComponentModel.DataAnnotations;

namespace Nexos_Network_System.ViewModels;

public class RetailShopFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(250)]
    [Display(Name = "Address Line 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "Address Line 2")]
    public string? AddressLine2 { get; set; }

    [Required, StringLength(30)]
    [Display(Name = "Contact Number")]
    public string ContactNumber { get; set; } = string.Empty;

    [Required, Display(Name = "City")]
    public int CityId { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
