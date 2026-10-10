using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static bool ExpandCartesianRanges(Chart chart, VisualRenderContext context, ChartRect plot,
        ChartRange range, ChartRange? secondaryRange, bool horizontalBars) {
        var changed = !horizontalBars && ExpandMarkerRanges(chart, context, plot, range, secondaryRange);
        // Both reservations mutate the domain and must run even when the first changes it.
        changed |= ExpandReferenceRanges(chart, context, plot, range, secondaryRange, horizontalBars);
        return changed;
    }

    private static bool ExpandReferenceRanges(Chart chart, VisualRenderContext context, ChartRect plot,
        ChartRange range, ChartRange? secondaryRange, bool horizontalBars) {
        var x = new MarkerExtents();
        var y = new MarkerExtents();
        // Horizontal-bar category positions use the mapper's linear category axis.
        var yAxis = horizontalBars ? new ChartAxis { Minimum = chart.Options.YAxis.Minimum, Maximum = chart.Options.YAxis.Maximum } : chart.Options.YAxis;
        foreach (var annotation in chart.Annotations) {
            if (annotation.EndValue.HasValue) continue;
            var radius = VisualReferenceStroke.PaintedWidth(annotation.StrokeStyle, annotation.Color, context.Theme.AxisStrokeWidth) / 2;
            if (radius <= 0) continue;
            if (annotation.Kind == ChartAnnotationKind.HorizontalLine) y.Include(annotation.Value, radius, yAxis);
            else x.Include(annotation.Value, radius, chart.Options.XAxis);
        }
        var changed = ExpandMarkerAxis(chart.Options.XAxis, x, plot.Width, range.MinX, range.MaxX, out var minX, out var maxX);
        range.SetXBounds(minX, maxX);
        secondaryRange?.SetXBounds(minX, maxX);
        changed |= ExpandMarkerAxis(yAxis, y, plot.Height, range.MinY, range.MaxY, out var minY, out var maxY);
        range.SetYBounds(minY, maxY);
        return changed;
    }
}
