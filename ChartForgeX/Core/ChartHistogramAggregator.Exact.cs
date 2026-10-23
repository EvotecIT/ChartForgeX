using System;
using System.Collections.Generic;
using System.Numerics;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

internal static partial class ChartHistogramAggregator {
    private const double SmallestNormalHistogramValue = 2.2250738585072014e-308;

    private static bool UnsafeHistogramIntermediate(double value) =>
        double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) < SmallestNormalHistogramValue;

    /// <summary>Recomputes only an exceptional bin, avoiding rounded normalized contributions.</summary>
    private static double ExactWeightedMean(ChartPoint[] observations, double[]? weights, List<int> indices) {
        var numerator = BigInteger.Zero; var denominator = BigInteger.Zero;
        foreach (var index in indices) {
            var sourceWeight = weights == null ? 1 : weights[index];
            if (sourceWeight == 0) continue;
            var weight = HistogramInteger(sourceWeight);
            numerator += HistogramInteger(observations[index].Y) * weight;
            denominator += weight;
        }
        var value = RoundHistogramRatio(numerator, denominator, -1074);
        ChartGuards.Finite(value, nameof(observations));
        return value;
    }

    /// <summary>Encodes one finite IEEE value in units of 2^-1074 without rounding.</summary>
    /// <remarks>
    /// This histogram-local fallback has no configurable precision. Finite input integers require
    /// at most 2098 bits; weighted sums require at most 4196 plus log2(observation count) bits.
    /// Ordinary bins retain the compensated double path and allocate none of these integers.
    /// </remarks>
    private static BigInteger HistogramInteger(double value) {
        var bits = BitConverter.DoubleToInt64Bits(value);
        var exponent = (int)((bits >> 52) & 0x7ff);
        var fraction = bits & 0x000fffffffffffffL;
        var magnitude = new BigInteger(exponent == 0 ? fraction : fraction | 0x0010000000000000L);
        if (exponent > 0) magnitude <<= exponent - 1;
        return bits < 0 ? -magnitude : magnitude;
    }

    /// <summary>Rounds the complete signed ratio once, including the subnormal ties-to-even boundary.</summary>
    private static double RoundHistogramRatio(BigInteger numerator, BigInteger denominator, int binaryScale) {
        if (numerator.IsZero) return 0;
        if (denominator.Sign <= 0) throw new InvalidOperationException("Histogram ratio requires positive mass.");
        var negative = numerator.Sign < 0;
        numerator = BigInteger.Abs(numerator);
        var ratioExponent = HistogramBitLength(numerator) - HistogramBitLength(denominator);
        var comparison = ratioExponent >= 0 ? numerator.CompareTo(denominator << ratioExponent)
            : (numerator << -ratioExponent).CompareTo(denominator);
        if (comparison < 0) ratioExponent--;
        var exponent = ratioExponent + binaryScale;
        if (exponent > 1023) return negative ? double.NegativeInfinity : double.PositiveInfinity;
        if (exponent < -1075) return 0;
        var unitExponent = Math.Max(-1074, exponent - 52);
        var shift = binaryScale - unitExponent;
        if (shift >= 0) numerator <<= shift;
        else denominator <<= -shift;
        var significand = BigInteger.DivRem(numerator, denominator, out var remainder);
        var midpoint = (remainder << 1).CompareTo(denominator);
        if (midpoint > 0 || midpoint == 0 && !significand.IsEven) significand++;
        var value = (double)significand * Math.Pow(2, unitExponent);
        return negative ? -value : value;
    }

    private static int HistogramBitLength(BigInteger value) {
        var bytes = value.ToByteArray();
        var index = bytes.Length - 1;
        while (index > 0 && bytes[index] == 0) index--;
        var top = bytes[index]; var bits = index * 8;
        while (top > 0) { bits++; top >>= 1; }
        return bits;
    }
}
