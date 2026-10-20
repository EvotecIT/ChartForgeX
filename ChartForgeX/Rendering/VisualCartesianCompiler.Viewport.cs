using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static bool ViewportObservationVisible(ChartXViewport? x, ChartYViewport? y, double sourceX, double minimumY, double maximumY) =>
        (x == null || sourceX >= x.Minimum && sourceX <= x.Maximum)
        && (y == null || maximumY >= y.Minimum && minimumY <= y.Maximum);

    private static bool ViewportLogicalYVisible(ChartYViewport? y, ChartRect plot, double first, double second) =>
        y == null || Math.Max(first, second) >= plot.Top && Math.Min(first, second) <= plot.Bottom;

    private static void ValidateFiniteYProjection(Chart chart, ChartMapper map) {
        foreach (var series in chart.Series) {
            var stride = ObservationStride(series.Kind);
            for (var raw = 0; raw < series.RenderPoints.Count; raw++) {
                if (series.Kind == ChartSeriesKind.Bubble && raw % stride != 0) continue;
                if (!ChartMath.IsFinite(map.Y(series.RenderPoints[raw].Y)))
                    throw new ArgumentOutOfRangeException("viewport", "The Y viewport cannot project source coordinates into finite logical units.");
            }
        }
    }
}
