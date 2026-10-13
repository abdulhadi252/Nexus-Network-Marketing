using System.Globalization;
using System.Text;

namespace Nexos_Network_System.Helpers;

/// <summary>
/// Pure geometry helpers behind the server-rendered SVG charts on the dashboards.
///
/// Charts are drawn at render time from live database values; there is no chart library and
/// no client-side JavaScript involved. Every number that ends up inside an SVG attribute is
/// formatted with the invariant culture, because a decimal comma would produce invalid markup.
/// </summary>
public static class ChartGeometry
{
    /// <summary>Formats a coordinate for use inside an SVG attribute (invariant culture).</summary>
    public static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>
    /// Projects a series into a width x height box. The scale is supplied by the caller so that
    /// several series can share one axis and stay visually comparable.
    /// </summary>
    public static IReadOnlyList<(double X, double Y)> Project(
        IReadOnlyList<double> values,
        double width,
        double height,
        double min,
        double max,
        double inset = 2)
    {
        var points = new List<(double X, double Y)>(values.Count);
        if (values.Count == 0)
        {
            return points;
        }

        var usableHeight = Math.Max(height - (inset * 2), 1d);
        var step = values.Count > 1 ? width / (values.Count - 1) : 0d;
        var span = max - min;

        for (var i = 0; i < values.Count; i++)
        {
            var x = values.Count > 1 ? i * step : width / 2d;
            var y = span <= 0
                ? height / 2d
                : inset + usableHeight - (((values[i] - min) / span) * usableHeight);
            points.Add((x, y));
        }

        return points;
    }

    /// <summary>SVG polyline points for a line series.</summary>
    public static string LinePoints(
        IReadOnlyList<double> values,
        double width,
        double height,
        double min,
        double max,
        double inset = 2)
    {
        var points = Project(values, width, height, min, max, inset);
        return string.Join(' ', points.Select(p => $"{N(p.X)},{N(p.Y)}"));
    }

    /// <summary>SVG path for a filled area series, closed along the bottom edge.</summary>
    public static string AreaPath(
        IReadOnlyList<double> values,
        double width,
        double height,
        double min,
        double max,
        double inset = 2)
    {
        var points = Project(values, width, height, min, max, inset);
        if (points.Count == 0)
        {
            return string.Empty;
        }

        var baseline = N(height);
        var path = new StringBuilder();
        path.Append("M ").Append(N(points[0].X)).Append(',').Append(baseline);

        foreach (var (x, y) in points)
        {
            path.Append(" L ").Append(N(x)).Append(',').Append(N(y));
        }

        path.Append(" L ").Append(N(points[^1].X)).Append(',').Append(baseline).Append(" Z");
        return path.ToString();
    }

    /// <summary>
    /// Builds a zero-based axis range with a friendly upper bound (1, 2 or 5 times a power of ten)
    /// so the gridlines land on readable numbers. Degenerate series still get a usable range.
    /// </summary>
    public static (double Min, double Max) NiceRange(IEnumerable<double> values)
    {
        var finite = values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToList();
        if (finite.Count == 0)
        {
            return (0d, 1d);
        }

        // A series with no movement would otherwise be drawn along an edge; give it room
        // to sit in the middle of the plot instead.
        if (finite.Distinct().Count() == 1)
        {
            var only = finite[0];
            var padding = Math.Max(Math.Abs(only) * 0.5, 1d);
            return (only - padding, only + padding);
        }

        var max = finite.Max();
        if (max <= 0)
        {
            return (0d, 1d);
        }

        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(max)));
        var normalized = max / magnitude;
        var niceFactor = normalized <= 1 ? 1d : normalized <= 2 ? 2d : normalized <= 5 ? 5d : 10d;
        return (0d, niceFactor * magnitude);
    }

    /// <summary>
    /// Builds a zero-based axis whose gridlines land on round numbers, returning the axis maximum
    /// and each tick with its position measured from the bottom of the plot (0 = 0, 1 = maximum).
    ///
    /// The step is chosen before the tick count so small ranges do not produce fractional ticks that
    /// all round to the same label. Set <paramref name="integerSteps"/> for count-like data.
    /// </summary>
    public static (double Min, double Max, IReadOnlyList<(double Value, double Fraction)> Ticks) NiceAxis(
        IEnumerable<double> values,
        int targetSteps = 4,
        bool integerSteps = true)
    {
        var highest = values
            .Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
            .DefaultIfEmpty(0d)
            .Max();

        // Nothing to plot yet: keep a plainly readable 0 - 1 axis.
        if (highest <= 0)
        {
            return (0d, 1d, new List<(double, double)> { (0d, 0d), (1d, 1d) });
        }

        var rawStep = highest / Math.Max(targetSteps, 1);
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
        var normalized = rawStep / magnitude;
        var step = (normalized <= 1 ? 1d : normalized <= 2 ? 2d : normalized <= 5 ? 5d : 10d) * magnitude;

        if (integerSteps)
        {
            step = Math.Max(Math.Ceiling(step), 1d);
        }

        var steps = Math.Max((int)Math.Ceiling(highest / step), 1);
        var axisMax = steps * step;

        var ticks = new List<(double Value, double Fraction)>(steps + 1);
        for (var i = 0; i <= steps; i++)
        {
            var value = i * step;
            ticks.Add((value, value / axisMax));
        }

        return (0d, axisMax, ticks);
    }

    /// <summary>
    /// Splits a donut into stroke-dash segments. Using dash lengths instead of arc paths keeps this
    /// exact and avoids the large-arc rounding pitfalls of hand-built SVG arcs.
    /// </summary>
    public static IReadOnlyList<(double Length, double Offset)> DonutSegments(
        IReadOnlyList<double> values,
        double radius,
        double gap = 0)
    {
        var segments = new List<(double Length, double Offset)>();
        var total = values.Sum();
        if (total <= 0 || radius <= 0)
        {
            return segments;
        }

        var circumference = 2 * Math.PI * radius;
        var offset = 0d;

        foreach (var value in values)
        {
            var share = value / total;
            var length = Math.Max((share * circumference) - gap, 0d);
            segments.Add((length, -offset));
            offset += share * circumference;
        }

        return segments;
    }
}
