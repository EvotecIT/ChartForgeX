using System;
using System.Collections.Generic;
using System.Numerics;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

internal static partial class ChartHistogramAggregator {
    private static void ValidateWeights(ChartPoint[] observations, double[]? weights) {
        if (weights == null) return;
        if (weights.Length != observations.Length) throw new ArgumentException("Histogram weights must match the source observations.", nameof(weights));
        foreach (var weight in weights) {
            ChartGuards.Finite(weight, nameof(weights));
            if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weights), "Histogram weights must be nonnegative.");
        }
    }

    private static double? AggregateWeightedMean(ChartPoint[] observations, double[]? weights, List<int> indices) {
        if (weights == null) return AggregateQuantity(observations, indices, ChartHistogramAggregation.Mean, exactIfUnderflow: true);
        var weightScale = 0d;
        var quantityScale = 0d;
        var minimum = double.PositiveInfinity;
        var maximum = double.NegativeInfinity;
        var uniform = indices.Count > 0 && weights[indices[0]] > 0;
        var firstWeight = indices.Count > 0 ? weights[indices[0]] : 0;
        foreach (var index in indices) {
            uniform &= weights[index] == firstWeight;
            weightScale = Math.Max(weightScale, weights[index]);
            if (weights[index] > 0) {
                quantityScale = Math.Max(quantityScale, Math.Abs(observations[index].Y));
                minimum = Math.Min(minimum, observations[index].Y);
                maximum = Math.Max(maximum, observations[index].Y);
            }
        }
        if (weightScale == 0) return null;
        // Equal positive weights are exactly the existing arithmetic-mean contract. Summing before
        // dividing also retains subnormal quantities that separate rounded contributions lose.
        if (uniform) return AggregateQuantity(observations, indices, ChartHistogramAggregation.Mean, exactIfUnderflow: true);
        if (quantityScale == 0) return 0;
        var denominator = 0d; var denominatorCorrection = 0d;
        foreach (var index in indices) {
            var normalized = weights[index] / weightScale;
            if (weights[index] > 0 && UnsafeHistogramIntermediate(normalized))
                return ExactWeightedMean(observations, weights, indices);
            AddCompensated(ref denominator, ref denominatorCorrection, normalized);
        }
        denominator += denominatorCorrection;
        // Accumulate normalized contributions rather than overflowing sum(weight * quantity).
        // A half scale also leaves headroom when rounding several near-MaxValue contributions.
        var quantityFactor = quantityScale > double.MaxValue / 2 ? .5 : 1;
        var sum = 0d; var correction = 0d;
        foreach (var index in indices) {
            var quantity = observations[index].Y * quantityFactor;
            var weight = weights[index];
            if (weight == 0) continue;
            // Multiplying first preserves a representable contribution when weight / scale underflows.
            var weighted = Math.Abs(quantity) > 1 && weight < 1
                ? (quantity * weight) / weightScale : quantity * (weight / weightScale);
            var contribution = weighted / denominator;
            if (observations[index].Y != 0 && (UnsafeHistogramIntermediate(quantity)
                || UnsafeHistogramIntermediate(weighted) || UnsafeHistogramIntermediate(contribution)))
                return ExactWeightedMean(observations, weights, indices);
            AddCompensated(ref sum, ref correction, contribution);
        }
        var value = (sum + correction) / quantityFactor;
        // Nonnegative weighting is a convex combination; roundoff cannot create a value outside its source range.
        value = Math.Max(minimum, Math.Min(maximum, value));
        ChartGuards.Finite(value, nameof(observations));
        return value;
    }

    private static double[] NormalizeMass(double?[] values, ChartHistogramBinLayout layout, bool density) {
        var scale = 0d;
        foreach (var value in values) scale = Math.Max(scale, value ?? 0);
        var result = new double[values.Length];
        if (scale == 0) return result;
        var total = 0d; var correction = 0d;
        foreach (var value in values) AddCompensated(ref total, ref correction, (value ?? 0) / scale);
        total += correction;
        BigInteger? exactTotal = null;
        for (var index = 0; index < result.Length; index++) {
            var value = values[index] ?? 0;
            var width = density ? layout.GetWidth(index) : 1;
            if (density && width <= 0) throw new ArgumentException("Histogram density requires positive-width intervals.", nameof(layout));
            if (!density) result[index] = (value / scale) / total;
            // A narrow interval can have representable density even when its probability underflows.
            // Divide by narrow widths first when finite; wide intervals can safely use normalized mass.
            var perWidth = density && width < 1 ? value / width : double.PositiveInfinity;
            if (density) {
                if (double.IsInfinity(perWidth)) result[index] = ((value / scale) / total) / width;
                else {
                    var scaled = perWidth / scale;
                    result[index] = double.IsInfinity(scaled) ? (perWidth / total) / scale : scaled / total;
                }
            }
            // Only exceptional exponent boundaries leave the fast double path. A final quotient
            // can remain finite even when value/width overflows or normalized mass underflows.
            if (value > 0 && (UnsafeHistogramIntermediate(result[index])
                || density && width < 1 && UnsafeHistogramIntermediate(perWidth))) {
                if (!exactTotal.HasValue) {
                    var captured = BigInteger.Zero;
                    foreach (var mass in values) captured += HistogramInteger(mass ?? 0);
                    exactTotal = captured;
                }
                result[index] = RoundHistogramRatio(HistogramInteger(value),
                    density ? exactTotal.Value * HistogramInteger(width) : exactTotal.Value, density ? 1074 : 0);
            }
        }
        return result;
    }

    private static void AddCompensated(ref double sum, ref double correction, double value) {
        var next = sum + value;
        correction += Math.Abs(sum) >= Math.Abs(value) ? (sum - next) + value : (value - next) + sum;
        sum = next;
    }
}
