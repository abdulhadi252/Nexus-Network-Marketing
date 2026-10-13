using System.ComponentModel.DataAnnotations;

namespace Nexos_Network_System.ViewModels;

public class ContactViewModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    [DataType(DataType.MultilineText)]
    public string Message { get; set; } = string.Empty;
}
