using System.Linq;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSeriesVisibilityCapture {
    // The painted SVG is the complete native layout/paint guard; these are the captured non-paint source-map facts.
    private static bool SameAxes(PreparedVisual first, PreparedVisual last) => first.CartesianOrientation == last.CartesianOrientation
        && SameAxis(first.XProjection, last.XProjection) && SameAxis(first.YProjection, last.YProjection)
        && SameAxis(first.SecondaryXProjection, last.SecondaryXProjection) && SameAxis(first.SecondaryYProjection, last.SecondaryYProjection);
    internal static bool SameAxis(ChartAxisProjection? first, ChartAxisProjection? last) {
        if (first == null || last == null) return first == null && last == null;
        if (first.Dimension != last.Dimension || first.Side != last.Side || !first.PlotBounds.Equals(last.PlotBounds)
            || first.FullMinimum != last.FullMinimum || first.FullMaximum != last.FullMaximum || first.Minimum != last.Minimum || first.Maximum != last.Maximum
            || first.MaximumInclusive != last.MaximumInclusive || first.Reversed != last.Reversed || first.Scale != last.Scale
            || first.Threshold != last.Threshold || first.LogarithmBase != last.LogarithmBase || first.LogarithmicDomain != last.LogarithmicDomain
            || first.TransformedMinimum != last.TransformedMinimum || first.TransformedMaximum != last.TransformedMaximum
            || first.TransformedFullMinimum != last.TransformedFullMinimum || first.TransformedFullMaximum != last.TransformedFullMaximum) return false;
        var a = first.TimeSessions; var b = last.TimeSessions;
        if ((a == null) != (b == null) || a != null && !a.Sessions.Select(row => (row.Minimum, row.Maximum)).SequenceEqual(b!.Sessions.Select(row => (row.Minimum, row.Maximum)))) return false;
        var x = first.TimeDomain; var y = last.TimeDomain;
        if (x == null || y == null) return x == null && y == null;
        if (x.Unit != y.Unit || x.FullMinimum != y.FullMinimum || x.FullMaximum != y.FullMaximum || !x.Instants.SequenceEqual(y.Instants)) return false;
        var u = x.Spacing.CalendarUnit; var v = y.Spacing.CalendarUnit;
        return u == null || v == null ? u == null && v == null : u.Unit == v.Unit && u.Count == v.Count && u.WeekStart == v.WeekStart;
    }
}
