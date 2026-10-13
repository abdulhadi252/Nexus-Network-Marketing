using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SearchController(IAdminSearchService searchService) : Controller
{
    public async Task<IActionResult> Index(SearchViewModel model)
    {
        ViewData["PanelName"] = "Admin Panel";

        var hasAnyFilter =
            !string.IsNullOrWhiteSpace(model.Query) ||
            !string.IsNullOrWhiteSpace(model.OrderNumber) ||
            !string.IsNullOrWhiteSpace(model.AccountNumber) ||
            !string.IsNullOrWhiteSpace(model.CustomerName) ||
            !string.IsNullOrWhiteSpace(model.ContactNumber) ||
            model.ConnectionType.HasValue ||
            model.OrderStatus.HasValue ||
            model.ConnectionStatus.HasValue ||
            model.From.HasValue ||
            model.To.HasValue;

        if (hasAnyFilter)
        {
            model = await searchService.SearchAsync(model);
        }

        return View(model);
    }
}
