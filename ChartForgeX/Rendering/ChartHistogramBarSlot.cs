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

    public static bool TryResolve(Chart chart, ChartBarCoordinateMap coordinateMap, ChartStackLayout stacks, int seriesIndex, int pointIndex, ChartMapper map, out double left, out double width) {
        var series = chart.Series[seriesIndex];
        if (pointIndex < 0 || pointIndex >= series.Points.Count || !coordinateMap.TryResolveHistogramSlot(seriesIndex, pointIndex, out var layout, out var binIndex) || layout.Minimum == layout.Maximum) {
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
        if (series.IsHistogramDensity) {
            left = binLeft; width = binWidth;
            return true;
        }
        var groupedSeries = coordinateMap.SeriesIndices(seriesIndex, pointIndex);
        var slot = stacks.Slot(groupedSeries, seriesIndex);
        var groupCount = slot.Count;
        var groupPosition = slot.Position;
        var inset = binWidth * OuterInsetRatio;
        var occupiedWidth = Math.Max(0, binWidth - inset * 2);
        var gap = groupCount == 1 ? 0 : occupiedWidth * GroupGapRatio / (groupCount - 1);
        width = Math.Max(1, (occupiedWidth - gap * (groupCount - 1)) / groupCount);
        left = binLeft + inset + groupPosition * (width + gap);
        return true;
    }

}
