using System.ComponentModel.DataAnnotations;

namespace Nexos_Network_System.ViewModels;

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required, StringLength(30)]
    [Display(Name = "Unit of Measure")]
    public string UnitOfMeasure { get; set; } = "Piece";

    [Range(0, 9999999)]
    [Display(Name = "Unit Cost")]
    public decimal UnitCost { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Sale Price")]
    public decimal? UnitSalePrice { get; set; }

    [Required, Display(Name = "Vendor")]
    public int VendorId { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Range(0, 1000000)]
    [Display(Name = "Opening Quantity")]
    public int OpeningQuantity { get; set; }

    [Range(0, 1000000)]
    [Display(Name = "Reorder Level")]
    public int ReorderLevel { get; set; }
}
