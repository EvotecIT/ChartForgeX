using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void IncludeReferenceExtents(Chart chart, VisualRenderContext context,
        MarkerExtents x, MarkerExtents y, ChartAxis yAxis) {
        foreach (var annotation in chart.Annotations) {
            if (annotation.EndValue.HasValue) continue;
            var radius = VisualReferenceStroke.PaintedWidth(annotation.StrokeStyle, annotation.Color, context.Theme.AxisStrokeWidth) / 2;
            if (radius <= 0) continue;
            if (annotation.Kind == ChartAnnotationKind.HorizontalLine) y.Include(annotation.Value, radius, yAxis);
            else x.Include(annotation.Value, radius, chart.Options.XAxis);
        }
    }
}
