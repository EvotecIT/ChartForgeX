using System;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Resolves the gauge's drawn value color for marks and legends.</summary>
internal static class ChartGaugeColor {
    public static ChartColor Resolve(Chart chart, ChartSeries series) {
        if (series.Color.HasValue) return series.Color.Value;
        var minimum = series.RenderPoints.Count == 0 ? 0 : series.RenderPoints[0].X;
        var maximum = series.RenderPoints.Count > 1 ? series.RenderPoints[1].X : 100;
        if (Math.Abs(maximum - minimum) < 0.000001) maximum = minimum + 1;
        var ratio = series.RenderPoints.Count == 0 ? 0 : (series.RenderPoints[0].Y - minimum) / (maximum - minimum);
        return ratio < 0.60 ? chart.Options.ResolvedTheme.Negative : ratio < 0.80 ? chart.Options.ResolvedTheme.Warning : chart.Options.ResolvedTheme.Positive;
    }
}
