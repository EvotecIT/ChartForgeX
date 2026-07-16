using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Resolves histogram bins into shared SVG/PNG bar slots while preserving numeric bin widths.
/// </summary>
internal static class ChartHistogramBarSlot {
    private const double OuterInsetRatio = 0.08;
    private const double GroupGapRatio = 0.04;

    public static bool TryResolve(Chart chart, int seriesIndex, int pointIndex, ChartMapper map, out double left, out double width) {
        var series = chart.Series[seriesIndex];
        var layout = series.HistogramBinLayout;
        if (layout == null || layout.Minimum == layout.Maximum || pointIndex < 0 || pointIndex >= layout.Count) {
            left = 0;
            width = 0;
            return false;
        }

        var lower = map.X(layout.GetLowerBound(pointIndex));
        var upper = map.X(layout.GetUpperBound(pointIndex));
        var binLeft = Math.Min(lower, upper);
        var binWidth = Math.Abs(upper - lower);
        var histogramSeries = HistogramSeries(chart);
        var groupCount = chart.Options.BarMode == ChartBarMode.Stacked ? 1 : Math.Max(1, histogramSeries.Count);
        var groupPosition = chart.Options.BarMode == ChartBarMode.Stacked ? 0 : Math.Max(0, histogramSeries.IndexOf(seriesIndex));
        var inset = binWidth * OuterInsetRatio;
        var occupiedWidth = Math.Max(0, binWidth - inset * 2);
        var gap = groupCount == 1 ? 0 : occupiedWidth * GroupGapRatio / (groupCount - 1);
        width = Math.Max(1, (occupiedWidth - gap * (groupCount - 1)) / groupCount);
        left = binLeft + inset + groupPosition * (width + gap);
        return true;
    }

    private static List<int> HistogramSeries(Chart chart) {
        var result = new List<int>();
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i].Kind == ChartSeriesKind.Bar && chart.Series[i].HistogramBinLayout != null) result.Add(i);
        }

        return result;
    }
}
