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
        if (pointIndex < 0 || pointIndex >= series.Points.Count || !TryResolveBin(chart, series, pointIndex, out var layout, out var binIndex) || layout.Minimum == layout.Maximum) {
            left = 0;
            width = 0;
            return false;
        }

        var lowerBound = layout.GetLowerBound(binIndex);
        var upperBound = layout.GetUpperBound(binIndex);
        var lower = map.X(lowerBound);
        var upper = map.X(upperBound);
        var binLeft = Math.Min(lower, upper);
        var binWidth = Math.Abs(upper - lower);
        var groupedSeries = GroupedSeries(chart, lowerBound, upperBound, layout.GetCenter(binIndex));
        var groupCount = chart.Options.BarMode == ChartBarMode.Stacked ? 1 : Math.Max(1, groupedSeries.Count);
        var groupPosition = chart.Options.BarMode == ChartBarMode.Stacked ? 0 : Math.Max(0, groupedSeries.IndexOf(seriesIndex));
        var inset = binWidth * OuterInsetRatio;
        var occupiedWidth = Math.Max(0, binWidth - inset * 2);
        var gap = groupCount == 1 ? 0 : occupiedWidth * GroupGapRatio / (groupCount - 1);
        width = Math.Max(1, (occupiedWidth - gap * (groupCount - 1)) / groupCount);
        left = binLeft + inset + groupPosition * (width + gap);
        return true;
    }

    private static bool TryResolveBin(Chart chart, ChartSeries series, int pointIndex, out ChartHistogramBinLayout layout, out int binIndex) {
        if (series.HistogramBinLayout != null) {
            layout = series.HistogramBinLayout;
            binIndex = pointIndex;
            return pointIndex < layout.Count;
        }

        var coordinate = series.Points[pointIndex].X;
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var candidate = chart.Series[seriesIndex].HistogramBinLayout;
            if (candidate == null) continue;
            if (coordinate < candidate.Minimum || coordinate > candidate.Maximum) continue;
            var candidateIndex = candidate.GetIndex(coordinate);
            if (!ChartMath.SameCoordinate(candidate.GetCenter(candidateIndex), coordinate)) continue;
            layout = candidate;
            binIndex = candidateIndex;
            return true;
        }

        layout = null!;
        binIndex = -1;
        return false;
    }

    private static List<int> GroupedSeries(Chart chart, double lowerBound, double upperBound, double center) {
        var result = new List<int>();
        for (var i = 0; i < chart.Series.Count; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.Bar) continue;
            if (series.HistogramBinLayout != null) {
                if (ContainsBin(series.HistogramBinLayout, lowerBound, upperBound, center)) result.Add(i);
                continue;
            }

            for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
                if (!ChartMath.SameCoordinate(series.Points[pointIndex].X, center)) continue;
                result.Add(i);
                break;
            }
        }

        return result;
    }

    private static bool ContainsBin(ChartHistogramBinLayout layout, double lowerBound, double upperBound, double center) {
        if (center < layout.Minimum || center > layout.Maximum) return false;
        var index = layout.GetIndex(center);
        return ChartMath.SameCoordinate(layout.GetLowerBound(index), lowerBound) && ChartMath.SameCoordinate(layout.GetUpperBound(index), upperBound);
    }
}
