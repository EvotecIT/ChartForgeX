using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

internal static class ChartBoxPlotStatistics {
    internal static ChartBoxPlotDistribution[] Calculate(IReadOnlyList<ChartBoxPlotSampleGroup> groups, ChartBoxPlotStatisticsOptions options) {
        var result = new ChartBoxPlotDistribution[groups.Count]; var offset = 0;
        for (var index = 0; index < groups.Count; index++) {
            result[index] = Calculate(groups[index], index, offset, options);
            offset = checked(offset + groups[index].Samples.Count);
        }
        return result;
    }

    private static ChartBoxPlotDistribution Calculate(ChartBoxPlotSampleGroup group, int index, int offset, ChartBoxPlotStatisticsOptions options) {
        var sorted = group.Samples.ToArray();
        Array.Sort(sorted);
        var q1 = Quantile(sorted, .25); var median = Quantile(sorted, .5); var q3 = Quantile(sorted, .75);
        double? inlierMinimum = null, inlierMaximum = null;
        var sources = new int[sorted.Length]; var outliers = new List<int>();
        for (var sample = 0; sample < group.Samples.Count; sample++) {
            var value = group.Samples[sample]; sources[sample] = checked(offset + sample);
            if (options.WhiskerPolicy == ChartBoxPlotWhiskerPolicy.Iqr && OutsideFence(value, q1, q3, options.IqrMultiplier)) outliers.Add(sources[sample]);
            else {
                inlierMinimum = inlierMinimum.HasValue ? Math.Min(inlierMinimum.Value, value) : value;
                inlierMaximum = inlierMaximum.HasValue ? Math.Max(inlierMaximum.Value, value) : value;
            }
        }
        var summary = new ChartBoxPlot(group.X, Math.Min(q1, inlierMinimum ?? q1), q1, median, q3, Math.Max(q3, inlierMaximum ?? q3));
        return new ChartBoxPlotDistribution(index, summary, sorted[0], sorted[sorted.Length - 1], inlierMinimum, inlierMaximum,
            sources, outliers.ToArray(), options);
    }

    // Compare a distance/span ratio rather than constructing possibly unrepresentable fences.
    private static bool OutsideFence(double value, double q1, double q3, double multiplier) {
        if (value >= q1 && value <= q3) return false;
        var boundary = value < q1 ? q1 : q3;
        var distance = Math.Abs(value - boundary); var span = q3 - q1;
        if (span == 0) return true;
        if (double.IsInfinity(distance) || double.IsInfinity(span)) {
            distance = Math.Abs(value / 2 - boundary / 2); span = q3 / 2 - q1 / 2;
        }
        return distance / span > multiplier;
    }

    private static double Quantile(IReadOnlyList<double> sorted, double probability) {
        if (sorted.Count == 1) return sorted[0];
        var position = (sorted.Count - 1) * probability;
        var lower = (int)Math.Floor(position); var upper = (int)Math.Ceiling(position);
        return ChartMath.InterpolateRange(sorted[lower], sorted[upper], position - lower);
    }
}
