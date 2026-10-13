using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Helpers;

public static class BillQueries
{
    public static IQueryable<Bill> LatestBillPerConnection(this IQueryable<Bill> bills) =>
        bills.Where(b => b.Status != BillStatus.Cancelled &&
            !b.Connection.Bills.Any(other =>
                other.Status != BillStatus.Cancelled &&
                (other.BillingPeriodEnd > b.BillingPeriodEnd ||
                 (other.BillingPeriodEnd == b.BillingPeriodEnd &&
                  (other.GeneratedAt > b.GeneratedAt ||
                   (other.GeneratedAt == b.GeneratedAt && other.Id > b.Id))))));

    public static decimal SumLatestOutstanding(this IEnumerable<Bill> bills) =>
        bills.Where(b => b.Status != BillStatus.Cancelled)
            .GroupBy(b => b.ConnectionId)
            .Select(group => group
                .OrderByDescending(b => b.BillingPeriodEnd)
                .ThenByDescending(b => b.GeneratedAt)
                .ThenByDescending(b => b.Id)
                .First())
            .Where(b => b.DueAmount > 0)
            .Sum(b => b.DueAmount);
}
