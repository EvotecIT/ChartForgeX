using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawAnnotations(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, ChartMapper map, VisualThemeColors colors, bool bands, List<LabelObstacle> obstacles) =>
        VisualAnnotationCompiler.Draw(chart.Annotations, context, builder, plot, map.X, map.Y, colors, bands, obstacles);
}
