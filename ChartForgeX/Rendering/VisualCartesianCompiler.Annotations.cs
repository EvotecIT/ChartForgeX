using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawAnnotations(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, ChartMapper map, VisualThemeColors colors, bool bands, List<LabelObstacle> obstacles,
        bool constrainXToPlot, ChartXViewport? xViewport = null) {
        var horizontalValues = ChartCartesianOrientation.IsHorizontal(chart);
        VisualAnnotationCompiler.Draw(chart.Annotations, context, builder, plot, map.X, map.Y, colors, bands, obstacles,
            kindSelector: annotation => ChartCartesianOrientation.AnnotationKind(annotation, horizontalValues),
            isVisible: constrainXToPlot ? annotation => IsAnnotationVisible(annotation, plot, map, xViewport,
                ChartCartesianOrientation.AnnotationKind(annotation, horizontalValues)) : null);
    }

    private static bool IsAnnotationVisible(ChartAnnotation annotation, ChartRect plot, ChartMapper map, ChartXViewport? xViewport, ChartAnnotationKind kind) {
        if (kind == ChartAnnotationKind.HorizontalLine || kind == ChartAnnotationKind.HorizontalBand) return true;
        var minimum = xViewport?.Minimum ?? plot.Left;
        var maximum = xViewport?.Maximum ?? plot.Right;
        var value = xViewport != null ? annotation.Value : map.X(annotation.Value);
        if (!annotation.EndValue.HasValue) return value >= minimum && value <= maximum;
        var end = xViewport != null ? annotation.EndValue.Value : map.X(annotation.EndValue.Value);
        return Math.Max(value, end) > minimum && Math.Min(value, end) < maximum;
    }
}
