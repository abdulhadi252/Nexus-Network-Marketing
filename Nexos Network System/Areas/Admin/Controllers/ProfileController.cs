using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Nexos_Network_System.Controllers;
using Nexos_Network_System.Data;
using Nexos_Network_System.Models;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProfileController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext db) : AccountProfileControllerBase(userManager, signInManager, db)
{
    protected override string PanelName => "Admin Panel";
}
