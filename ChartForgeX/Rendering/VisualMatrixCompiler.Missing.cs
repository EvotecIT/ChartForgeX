using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMatrixCompiler {
    // A missing position is part of the grid, but is neither a measured value nor an interactive target.
    private static void MissingCells(Chart chart, VisualSceneBuilder builder, ChartSeries series, double[] columns,
        ChartRect plot, double top, double width, double height, double gap, double radius, VisualThemeColors colors) {
        var present = new HashSet<double>(series.RenderPoints.Select(point => point.X));
        for (var column = 0; column < columns.Length; column++) {
            if (present.Contains(columns[column])) continue;
            var left = plot.Left + column * (width + gap);
            if (ReverseColumns(chart)) left = plot.Left + plot.Right - left - width;
            var bounds = new ChartRect(left, top, width, height);
            builder.Rect(bounds, null, colors.Border, .75, radius, "heatmap-missing-cell",
                paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Grid));
        }
    }
}
