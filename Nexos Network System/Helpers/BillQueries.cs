using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Helpers;

public static class BillQueries
{
    public static IQueryable<Bill> LatestBillPerConnection(this IQueryable<Bill> bills) =>
        bills.LatestBillPerConnection(asOf: null);

    /// <summary>
    /// The latest bill per connection as it stood at <paramref name="asOf"/>. Bills generated after
    /// that moment are excluded on both sides of the comparison, so the result is a true snapshot of
    /// what each connection owed at the time - which is what the outstanding trend is drawn from.
    /// Pass null for the current position.
    /// </summary>
    public static IQueryable<Bill> LatestBillPerConnection(this IQueryable<Bill> bills, DateTimeOffset? asOf)
    {
        if (asOf is null)
        {
            return bills.Where(b => b.Status != BillStatus.Cancelled &&
                !b.Connection.Bills.Any(other =>
                    other.Status != BillStatus.Cancelled &&
                    (other.BillingPeriodEnd > b.BillingPeriodEnd ||
                     (other.BillingPeriodEnd == b.BillingPeriodEnd &&
                      (other.GeneratedAt > b.GeneratedAt ||
                       (other.GeneratedAt == b.GeneratedAt && other.Id > b.Id))))));
        }

        var cutoff = asOf.Value;
        return bills.Where(b => b.Status != BillStatus.Cancelled &&
            b.GeneratedAt <= cutoff &&
            !b.Connection.Bills.Any(other =>
                other.Status != BillStatus.Cancelled &&
                other.GeneratedAt <= cutoff &&
                (other.BillingPeriodEnd > b.BillingPeriodEnd ||
                 (other.BillingPeriodEnd == b.BillingPeriodEnd &&
                  (other.GeneratedAt > b.GeneratedAt ||
                   (other.GeneratedAt == b.GeneratedAt && other.Id > b.Id))))));
    }

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
