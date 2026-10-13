using Microsoft.AspNetCore.Identity;

namespace Nexos_Network_System.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}
