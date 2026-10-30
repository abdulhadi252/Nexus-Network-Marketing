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
                var pendingFeedback = await db.FeedbackEntries.CountAsync(f => !f.IsReviewed);

                Add(items, "Pending orders", pendingOrders,
                    Url.Action("Index", "Orders", new { area, status = OrderStatus.Pending }));
                Add(items, "Low stock items", lowStock,
                    Url.Action("Index", "Stock", new { area, lowOnly = true }));
                Add(items, "Unread messages", unreadMessages,
                    Url.Action("Index", "Messages", new { area, unreadOnly = true }));
                Add(items, "Feedback awaiting review", pendingFeedback,
                    Url.Action("Index", "Feedback", new { area, status = "pending" }));
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
                var forwardedJobs = await db.Orders.CountAsync(o =>
                    o.Status == OrderStatus.VerifiedByRetail && o.FeasibilityReport == null);
                var pendingReports = await db.FeasibilityReports.CountAsync(f => f.Status == FeasibilityStatus.Pending);
                var pendingConnections = await db.Connections.CountAsync(c => c.Status == ConnectionStatus.Pending);

                Add(items, "New jobs from retail", forwardedJobs,
                    Url.Action("Index", "Orders", new { area }));
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
                var pendingPayments = await db.Payments.CountAsync(p => p.Status == PaymentStatus.PendingVerification);

                Add(items, "Online payments to verify", pendingPayments,
                    Url.Action("Index", "Payments", new { area, status = PaymentStatus.PendingVerification }));
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

                // Scoped to the latest bill per connection, exactly like the dashboard's outstanding
                // figure, so the bell and the page it points at always agree.
                var unpaidBills = await db.Bills.LatestBillPerConnection()
                    .CountAsync(b => b.CustomerId == customerId.Value && b.DueAmount > 0);
                var pendingOrders = await db.Orders.CountAsync(o => o.CustomerId == customerId.Value &&
                    (o.Status == OrderStatus.Pending || o.Status == OrderStatus.FeasibilityPending));
                var pendingPayments = await db.Payments.CountAsync(p =>
                    p.CustomerId == customerId.Value && p.Status == PaymentStatus.PendingVerification);

                // Messages the team sent to this customer (e.g. "your bill is ready").
                var unreadNotifications = await db.CustomerNotifications.CountAsync(n =>
                    n.CustomerId == customerId.Value && !n.IsRead);

                Add(items, "Unpaid bills", unpaidBills,
                    Url.Action("Index", "Bills", new { area }));
                Add(items, "Payments awaiting confirmation", pendingPayments,
                    Url.Action("Index", "Payments", new { area }));
                Add(items, "Orders in progress", pendingOrders,
                    Url.Action("Index", "Orders", new { area }));
                Add(items, "New notifications", unreadNotifications,
                    Url.Action("Index", "Notifications", new { area }));
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