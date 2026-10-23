using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>Assigns observations once through the canonical layout and preserves raw aggregates separately from height encoding.</summary>
internal static partial class ChartHistogramAggregator {
    internal static ChartHistogramBin[] Aggregate(ChartPoint[] observations, ChartHistogramBinLayout layout,
        ChartHistogramAggregation aggregation, ChartHistogramEncoding encoding, double[]? weights = null) {
        if (!Enum.IsDefined(typeof(ChartHistogramAggregation), aggregation)) throw new ArgumentOutOfRangeException(nameof(aggregation));
        if (!Enum.IsDefined(typeof(ChartHistogramEncoding), encoding)) throw new ArgumentOutOfRangeException(nameof(encoding));
        ValidateWeights(observations, weights);
        var probability = encoding == ChartHistogramEncoding.Probability || encoding == ChartHistogramEncoding.ProbabilityDensity;
        if (probability && aggregation != ChartHistogramAggregation.Count && aggregation != ChartHistogramAggregation.Sum)
            throw new ArgumentException("Probability encoding requires Count or nonnegative Sum aggregates.", nameof(encoding));
        var indices = new List<int>[layout.Count];
        for (var bin = 0; bin < indices.Length; bin++) indices[bin] = new List<int>();
        for (var source = 0; source < observations.Length; source++) {
            if (observations[source].BreakBefore) throw new ArgumentException("Histogram observations do not support segment breaks.", nameof(observations));
            if (probability && aggregation == ChartHistogramAggregation.Sum && observations[source].Y < 0)
                throw new ArgumentException("Probability Sum observations must have nonnegative quantities.", nameof(observations));
            indices[layout.GetIndex(observations[source].X)].Add(source);
        }
        var values = new double?[layout.Count];
        for (var index = 0; index < values.Length; index++)
            values[index] = aggregation == ChartHistogramAggregation.Count ? indices[index].Count
                : aggregation == ChartHistogramAggregation.WeightedMean ? AggregateWeightedMean(observations, weights, indices[index])
                : AggregateQuantity(observations, indices[index], aggregation);
        var normalized = probability ? NormalizeMass(values, layout, encoding == ChartHistogramEncoding.ProbabilityDensity) : null;
        var bins = new ChartHistogramBin[layout.Count];
        for (var index = 0; index < bins.Length; index++) {
            var value = values[index];
            var density = encoding == ChartHistogramEncoding.Density || encoding == ChartHistogramEncoding.ProbabilityDensity;
            if (density && layout.GetWidth(index) <= 0)
                throw new ArgumentException("Histogram density requires positive-width intervals.", nameof(layout));
            var rendered = normalized == null ? value ?? 0 : normalized[index];
            if (density && !probability) rendered /= layout.GetWidth(index);
            ChartGuards.Finite(rendered, nameof(encoding));
            bins[index] = new ChartHistogramBin(layout, index, value, rendered, indices[index].ToArray());
        }
        return bins;
    }

    private static double? AggregateQuantity(ChartPoint[] observations, List<int> indices, ChartHistogramAggregation aggregation,
        bool exactIfUnderflow = false) {
        if (indices.Count == 0) return aggregation == ChartHistogramAggregation.Mean ? (double?)null : 0;
        // Scaling before compensated summation avoids overflowing a finite mean or a sum whose signed quantities cancel.
        var scale = 0d;
        foreach (var index in indices) scale = Math.Max(scale, Math.Abs(observations[index].Y));
        if (scale == 0) return 0;
        var sum = 0d; var correction = 0d;
        foreach (var index in indices) {
            var quantity = observations[index].Y / scale;
            if (exactIfUnderflow && observations[index].Y != 0 && UnsafeHistogramIntermediate(quantity))
                return ExactWeightedMean(observations, null, indices);
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
