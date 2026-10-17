using Microsoft.EntityFrameworkCore;

namespace Nexos_Network_System.Helpers;

/// <summary>
/// Buckets timestamped rows into one value per month of a fixed window. The dashboards and the
/// billing list all draw their sparklines from these series, so the arithmetic lives in one place.
/// </summary>
public static class MonthlySeries
{
    /// <summary>The first moment of the month that contains <paramref name="moment"/>.</summary>
    public static DateTimeOffset StartOfMonth(DateTimeOffset moment) =>
        new(moment.Year, moment.Month, 1, 0, 0, 0, moment.Offset);

    /// <summary>Index of a moment inside the window; -1 when it falls outside it.</summary>
    public static int Index(DateTimeOffset moment, DateTimeOffset windowStart) =>
        ((moment.Year - windowStart.Year) * 12) + (moment.Month - windowStart.Month);

    /// <summary>Counts moments into one bucket per month of the window; anything outside is ignored.</summary>
    public static List<int> Counts(IEnumerable<DateTimeOffset?> moments, DateTimeOffset windowStart, int months)
    {
        var buckets = new int[months];

        foreach (var moment in moments)
        {
            if (moment is null)
            {
                continue;
            }

            var index = Index(moment.Value, windowStart);
            if (index >= 0 && index < months)
            {
                buckets[index]++;
            }
        }

        return buckets.ToList();
    }

    /// <inheritdoc cref="Counts(IEnumerable{DateTimeOffset?}, DateTimeOffset, int)"/>
    public static async Task<List<int>> CountsAsync(
        IQueryable<DateTimeOffset> moments,
        DateTimeOffset windowStart,
        int months,
        CancellationToken cancellationToken = default)
    {
        var values = await moments.ToListAsync(cancellationToken);
        return Counts(values.Select(m => (DateTimeOffset?)m), windowStart, months);
    }

    /// <summary>Totals amounts into one bucket per month of the window.</summary>
    public static List<double> Sums(
        IEnumerable<(DateTimeOffset Moment, double Amount)> rows,
        DateTimeOffset windowStart,
        int months)
    {
        var buckets = new double[months];

        foreach (var (moment, amount) in rows)
        {
            var index = Index(moment, windowStart);
            if (index >= 0 && index < months)
            {
                buckets[index] += amount;
            }
        }

        return buckets.ToList();
    }

    /// <summary>Running total of a monthly series, starting from <paramref name="baseline"/>.</summary>
    public static List<int> Cumulative(IReadOnlyList<int> monthly, int baseline)
    {
        var running = baseline;
        var totals = new List<int>(monthly.Count);

        foreach (var value in monthly)
        {
            running += value;
            totals.Add(running);
        }

        return totals;
    }

    /// <summary>The last <paramref name="count"/> points of a series, as doubles for the chart helpers.</summary>
    public static List<double> Tail(IReadOnlyList<int> values, int count) =>
        values.Skip(Math.Max(values.Count - count, 0)).Select(value => (double)value).ToList();


    /// <inheritdoc cref="Tail(IReadOnlyList{int}, int)"/>
    public static List<double> Tail(IReadOnlyList<double> values, int count) =>
        values.Skip(Math.Max(values.Count - count, 0)).ToList();
}
