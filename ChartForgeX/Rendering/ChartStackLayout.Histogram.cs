using System;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartStackLayout {
    private void ValidateHistogramDensity(Chart chart, ChartBarCoordinateMap coordinates) {
        for (var index = 0; index < chart.Series.Count; index++) {
            var density = chart.Series[index];
            if (!density.IsHistogramDensity) continue;
            if (density.NormalizedTo.HasValue)
                throw new InvalidOperationException("Histogram density cannot be normalized because rectangular area must retain the raw aggregate.");
            for (var point = 0; point < density.Points.Count; point++) {
                if (Slot(coordinates.SeriesIndices(index, point), index).Count != 1)
                    throw new InvalidOperationException("Histogram density requires a single full-interval bar or stack; independent grouped slots cannot encode area truthfully.");
            }
            for (var other = index + 1; other < chart.Series.Count; other++) {
                var series = chart.Series[other];
                if (series.HistogramBinLayout == null) continue;
                ValidateDensityIntervals(density, series);
            }
            // Earlier value histograms also participate in overlapping physical intervals.
            for (var other = 0; other < index; other++)
                if (chart.Series[other].HistogramBinLayout != null && !chart.Series[other].IsHistogramDensity)
                    ValidateDensityIntervals(density, chart.Series[other]);
        }
    }

    private static void ValidateDensityIntervals(ChartSeries density, ChartSeries other) {
        var left = density.HistogramBinLayout!; var right = other.HistogramBinLayout!;
        var leftIndex = 0; var rightIndex = 0;
        while (leftIndex < left.Count && rightIndex < right.Count) {
            var lower = Math.Max(left.GetLowerBound(leftIndex), right.GetLowerBound(rightIndex));
            var upper = Math.Min(left.GetUpperBound(leftIndex), right.GetUpperBound(rightIndex));
            if (lower < upper && (!other.IsHistogramDensity || density.YAxis != other.YAxis ||
                left.GetLowerBound(leftIndex) != right.GetLowerBound(rightIndex) ||
                left.GetUpperBound(leftIndex) != right.GetUpperBound(rightIndex)))
                throw new InvalidOperationException("Overlapping histogram density intervals must use identical bounds and a compatible density stack on the same axis.");
            if (left.GetUpperBound(leftIndex) <= right.GetUpperBound(rightIndex)) leftIndex++;
            else rightIndex++;
        }
    }
}
