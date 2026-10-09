using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>Assigns observations once through the canonical layout and preserves raw aggregates separately from height encoding.</summary>
internal static class ChartHistogramAggregator {
    internal static ChartHistogramBin[] Aggregate(ChartPoint[] observations, ChartHistogramBinLayout layout,
        ChartHistogramAggregation aggregation, ChartHistogramEncoding encoding) {
        if (!Enum.IsDefined(typeof(ChartHistogramAggregation), aggregation)) throw new ArgumentOutOfRangeException(nameof(aggregation));
        if (!Enum.IsDefined(typeof(ChartHistogramEncoding), encoding)) throw new ArgumentOutOfRangeException(nameof(encoding));
        var indices = new List<int>[layout.Count];
        for (var bin = 0; bin < indices.Length; bin++) indices[bin] = new List<int>();
        for (var source = 0; source < observations.Length; source++) {
            if (observations[source].BreakBefore) throw new ArgumentException("Histogram observations do not support segment breaks.", nameof(observations));
            indices[layout.GetIndex(observations[source].X)].Add(source);
        }
        var bins = new ChartHistogramBin[layout.Count];
        for (var index = 0; index < bins.Length; index++) {
            double? value = aggregation == ChartHistogramAggregation.Count ? indices[index].Count
                : AggregateQuantity(observations, indices[index], aggregation);
            if (encoding == ChartHistogramEncoding.Density && layout.GetWidth(index) <= 0)
                throw new ArgumentException("Histogram density requires positive-width intervals.", nameof(layout));
            var rendered = encoding == ChartHistogramEncoding.Density ? (value ?? 0) / layout.GetWidth(index) : value ?? 0;
            ChartGuards.Finite(rendered, nameof(encoding));
            bins[index] = new ChartHistogramBin(layout, index, value, rendered, indices[index].ToArray());
        }
        return bins;
    }

    private static double? AggregateQuantity(ChartPoint[] observations, List<int> indices, ChartHistogramAggregation aggregation) {
        if (indices.Count == 0) return aggregation == ChartHistogramAggregation.Mean ? (double?)null : 0;
        // Scaling before compensated summation avoids overflowing a finite mean or a sum whose signed quantities cancel.
        var scale = 0d;
        foreach (var index in indices) scale = Math.Max(scale, Math.Abs(observations[index].Y));
        if (scale == 0) return 0;
        var sum = 0d; var correction = 0d;
        foreach (var index in indices) {
            var quantity = observations[index].Y / scale;
            var next = sum + quantity;
            correction += Math.Abs(sum) >= Math.Abs(quantity) ? (sum - next) + quantity : (quantity - next) + sum;
            sum = next;
        }
        sum += correction;
        var value = (aggregation == ChartHistogramAggregation.Mean ? sum / indices.Count : sum) * scale;
        ChartGuards.Finite(value, nameof(observations));
        return value;
    }
}
