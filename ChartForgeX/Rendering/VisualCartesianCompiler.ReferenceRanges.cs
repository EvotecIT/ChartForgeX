using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void IncludeReferenceExtents(Chart chart, VisualRenderContext context,
        MarkerExtents x, MarkerExtents y, ChartAxis yAxis) {
        var horizontalValues = ChartCartesianOrientation.IsHorizontal(chart);
        foreach (var annotation in chart.Annotations) {
            if (annotation.IsBand || annotation.HasSourceGeometry) continue;
            var radius = VisualReferenceStroke.PaintedWidth(annotation.StrokeStyle, annotation.Color, context.Theme.AxisStrokeWidth) / 2;
            if (radius <= 0) continue;
            var kind = ChartCartesianOrientation.AnnotationKind(annotation, horizontalValues);
            if (kind == ChartAnnotationKind.HorizontalLine) y.Include(annotation.Value, radius, yAxis);
            else x.Include(annotation.Value, radius, chart.Options.XAxis);
        }
    }
}
