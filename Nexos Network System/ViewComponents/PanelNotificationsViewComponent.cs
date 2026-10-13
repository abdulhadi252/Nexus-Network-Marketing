using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.ViewComponents;

/// <summary>
/// Backs the bell in the panel topbar.
///
/// The counts mirror what the signed-in user's own dashboard reports and use the same scoping
/// rules, so the bell can never disagree with the page it points at. Entries with a count of
/// zero are dropped rather than shown as an empty row.
/// </summary>
public class PanelNotificationsViewComponent(ApplicationDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        // ViewComponent.User is typed as IPrincipal; the current-user helpers need the
        // ClaimsPrincipal the framework actually supplies.
        if (User is not ClaimsPrincipal principal)
        {
            return View(new PanelNotificationsViewModel());
        }

        var area = principal.GetPanelAreaName();
        var items = new List<PanelNotification>();

        if (area is null)
        {
            return View(new PanelNotificationsViewModel());
        }

        switch (area)
        {
            case "Admin":
            {
                var pendingOrders = await db.Orders.CountAsync(o =>
                    o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending);
                var lowStock = await db.StockItems.CountAsync(s => s.QuantityOnHand <= s.ReorderLevel);
                var unreadMessages = await db.ContactMessages.CountAsync(m => !m.IsRead);

                Add(items, "Pending orders", pendingOrders,
                    Url.Action("Index", "Orders", new { area, status = OrderStatus.Pending }));
                Add(items, "Low stock items", lowStock,
                    Url.Action("Index", "Stock", new { area, lowOnly = true }));
                Add(items, "Unread messages", unreadMessages,
                    Url.Action("Index", "Messages", new { area, unreadOnly = true }));
                break;
            }

            case "Retail":
            {
                var shopId = await principal.GetRetailShopIdAsync(db);
                if (shopId is null)
                {
                    // The retail dashboard refuses the same way when no active shop is assigned.
                    return View(new PanelNotificationsViewModel());
                }

                var pendingOrders = await db.Orders.CountAsync(o => o.RetailShopId == shopId.Value &&
                    (o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending));
                var dues = await db.Bills.LatestBillPerConnection().CountAsync(b => b.DueAmount > 0 &&
                    b.Connection.Order != null && b.Connection.Order.RetailShopId == shopId.Value);

                Add(items, "Pending orders", pendingOrders,
                    Url.Action("Index", "Orders", new { area }));
                Add(items, "Bills with dues", dues,
                    Url.Action("Index", "Bills", new { area }));
                break;
            }

            case "Technical":
            {
                var pendingReports = await db.FeasibilityReports.CountAsync(f => f.Status == FeasibilityStatus.Pending);
                var pendingConnections = await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Pending);

                Add(items, "Feasibility reports awaiting a check", pendingReports,
                    Url.Action("Index", "Feasibility", new { area }));
                Add(items, "Connections awaiting installation", pendingConnections,
                    Url.Action("Index", "Connections", new { area }));
                break;
            }

            case "Accounts":
            {
                var dueBills = await db.Bills.LatestBillPerConnection().CountAsync(b => b.DueAmount > 0);
                var unpaidBills = await db.Bills.LatestBillPerConnection().CountAsync(b => b.Status == BillStatus.Unpaid);

                Add(items, "Bills with dues", dueBills,
                    Url.Action("Index", "Bills", new { area }));
                Add(items, "Unpaid bills", unpaidBills,
                    Url.Action("Index", "Dues", new { area }));
                break;
            }

            case "Customer":
            {
                var customerId = await principal.GetCustomerIdAsync(db);
                if (customerId is null)
                {
                    return View(new PanelNotificationsViewModel());
                }

                var unpaidBills = await db.Bills.CountAsync(b => b.CustomerId == customerId.Value && b.DueAmount > 0);
                var pendingOrders = await db.Orders.CountAsync(o => o.CustomerId == customerId.Value &&
                    (o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending));

                Add(items, "Unpaid bills", unpaidBills,
                    Url.Action("Index", "Bills", new { area }));
                Add(items, "Orders in progress", pendingOrders,
                    Url.Action("Index", "Orders", new { area }));
                break;
            }
        }

        return View(new PanelNotificationsViewModel { Items = items });
    }

    private static void Add(List<PanelNotification> items, string label, int count, string? url)
    {
        if (count > 0)
        {
            items.Add(new PanelNotification { Label = label, Count = count, Url = url });
        }
    }
}
