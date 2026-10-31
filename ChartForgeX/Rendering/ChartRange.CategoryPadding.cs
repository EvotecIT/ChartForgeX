using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartRange {
    /// <summary>
    /// Outer-padding rule for named categories: a line, area or strip of points over authored category labels keeps half a band before the
    /// first and after the last category, as bars do, so its first and last points never sit on the plot edge.
    /// Continuous axes keep their marker-safe padding (<see cref="VisualCartesianCompiler"/> marker expansion) instead.
    /// </summary>
    private void ApplyCategoryPadding(Chart chart) {
        var axis = chart.Options.XAxis;
        if (axis.Scale != ChartScaleKind.Linear || axis.Labels.Count == 0 || axis.PreparedTimeDomain != null || axis.TimeSessions != null) return;
        if (axis.Minimum.HasValue || axis.Maximum.HasValue || chart.Options.IsSparkline) return;
        if (chart.Series.Count == 0 || !chart.Series.All(series => IsCategoryLine(series.Kind))) return;
        var values = chart.Series.SelectMany(series => series.RenderPoints.Select(point => point.X)).Where(ChartMath.IsFinite).Distinct().ToList();
        if (values.Count < 2) return;
        var padding = CategoryPadding(values);
        MinX -= padding;
        MaxX += padding;
    }

    private static bool IsCategoryLine(ChartSeriesKind kind) => kind is ChartSeriesKind.Line or ChartSeriesKind.StepLine
        or ChartSeriesKind.Area or ChartSeriesKind.StepArea or ChartSeriesKind.StackedArea or ChartSeriesKind.Scatter;
}
