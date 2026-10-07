using System;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Resolves the gauge's drawn value color for marks and legends.</summary>
internal static class ChartGaugeColor {
    public static ChartColor Resolve(Chart chart, ChartSeries series) {
        if (series.Color.HasValue) return series.Color.Value;
        var minimum = series.Points.Count == 0 ? 0 : series.Points[0].X;
        var maximum = series.Points.Count > 1 ? series.Points[1].X : 100;
        if (Math.Abs(maximum - minimum) < 0.000001) maximum = minimum + 1;
        var ratio = series.Points.Count == 0 ? 0 : (series.Points[0].Y - minimum) / (maximum - minimum);
        return ratio < 0.60 ? chart.Options.Theme.Negative : ratio < 0.80 ? chart.Options.Theme.Warning : chart.Options.Theme.Positive;
    }
}
