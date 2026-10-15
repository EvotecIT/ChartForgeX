using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static class ChartTicks {
    private const int MaximumGeneratedTicks = 10_000;
    private const double DirectMagnitudeLimit = 1e150;

    /// <summary>Uses authored tick positions when present, otherwise the canonical bounded scale ticks.</summary>
    internal static IReadOnlyList<double> ForAxis(ChartAxis axis, double minimum, double maximum) =>
        axis.Labels.Count > 0 ? axis.Labels.Select(label => label.Value).Where(value => value >= minimum && value <= maximum).Distinct().OrderBy(value => value).ToArray()
            : GenerateInside(axis, minimum, maximum);

    /// <summary>Keeps generated numeric value ticks alongside authored label positions within the visible domain.</summary>
    internal static IReadOnlyList<double> ForValueAxis(ChartAxis axis, double minimum, double maximum) {
        var generated = GenerateInside(axis, minimum, maximum);
        if (axis.Labels.Count == 0) return generated;
        return PreserveFormatting(generated, generated.Concat(axis.Labels.Select(label => label.Value))
            .Where(value => value >= minimum && value <= maximum).Distinct().OrderBy(value => value).ToArray());
    }

    public static IReadOnlyList<double> Generate(ChartAxis axis, double min, double max) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (axis.Scale == ChartScaleKind.Time) return ChartTimeScale.Generate(axis, min, max, false) ?? new NumericTimeTicks(Generate(min, max, axis.TickCount));
        if (axis.Scale == ChartScaleKind.Logarithmic) return GenerateLogarithmic(min, max, axis.TickCount, false);
        if (axis.Scale != ChartScaleKind.SymmetricLogarithmic) return axis.Minimum.HasValue || axis.Maximum.HasValue ? GenerateInside(min, max, axis.TickCount) : Generate(min, max, axis.TickCount);
        return GenerateTransformed(axis, min, max, axis.TickCount, false);
    }

    public static IReadOnlyList<double> GenerateInside(ChartAxis axis, double min, double max) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (axis.Scale == ChartScaleKind.Time) return ChartTimeScale.Generate(axis, min, max, true) ?? new NumericTimeTicks(GenerateInside(min, max, axis.TickCount));
        if (axis.Scale == ChartScaleKind.Logarithmic) return GenerateLogarithmic(min, max, axis.TickCount, true);
        if (axis.Scale != ChartScaleKind.SymmetricLogarithmic) return GenerateInside(min, max, axis.TickCount);
        return GenerateTransformed(axis, min, max, axis.TickCount, true);
    }

    public static IReadOnlyList<double> Generate(double min, double max, int desiredCount) {
        desiredCount = Math.Min(MaximumGeneratedTicks, Math.Max(2, desiredCount));
        if (!TryNormalize(ref min, ref max, out var scale, out var normalizedMin, out var normalizedMax)) return new[] { 0d, 1d };
        if (min == max) return ExpandEqualValue(min);

        var normalizedStep = ChartNiceNumbers.Step((normalizedMax - normalizedMin) / (desiredCount - 1));
        if ((normalizedMax - normalizedMin) / normalizedStep >= MaximumGeneratedTicks - 1)
            normalizedStep = ChartNiceNumbers.Step((normalizedMax - normalizedMin) / (MaximumGeneratedTicks - 1));
        var step = normalizedStep * scale;
        if (!IsPositiveFinite(normalizedStep) || !IsPositiveFinite(step)) return DistinctEndpoints(min, max);

        var niceMin = Math.Floor(normalizedMin / normalizedStep) * normalizedStep;
        var niceMax = Math.Ceiling(normalizedMax / normalizedStep) * normalizedStep;
        // Rounding both ends outward can add another interval. Increase the shared
        // step rather than clipping the envelope or drawing more ticks than asked.
        for (var attempt = 0; (niceMax - niceMin) / normalizedStep > desiredCount - 1 + 1e-9; attempt++) {
            if (attempt >= 32 || desiredCount == 2) return DistinctEndpoints(min, max);
            normalizedStep = ChartNiceNumbers.Step(normalizedStep * 1.01);
            if (!IsPositiveFinite(normalizedStep)) return DistinctEndpoints(min, max);
            niceMin = Math.Floor(normalizedMin / normalizedStep) * normalizedStep;
            niceMax = Math.Ceiling(normalizedMax / normalizedStep) * normalizedStep;
        }
        var ticks = new List<double>();
        for (var index = 0; index < desiredCount; index++) {
            var normalizedValue = niceMin + normalizedStep * index;
            if (normalizedValue > niceMax + normalizedStep * 0.5) break;
            AddFiniteDistinct(ticks, Denormalize(NormalizeZero(normalizedValue, normalizedStep), scale, min, max, normalizedMin, normalizedMax));
        }

        PreserveUpperEndpoint(ticks, Denormalize(NormalizeZero(niceMax, normalizedStep), scale, min, max, normalizedMin, normalizedMax), desiredCount);

        return ticks.Count >= 2 ? ticks : DistinctEndpoints(min, max);
    }

    internal static bool IsNumericTimeFallback(IReadOnlyList<double> ticks) => ticks is NumericTimeTicks;

    internal static IReadOnlyList<double> PreserveFormatting(IReadOnlyList<double> source, IReadOnlyList<double> selected) =>
        IsNumericTimeFallback(source) ? new NumericTimeTicks(selected) : selected;

    private sealed class NumericTimeTicks : List<double> {
        public NumericTimeTicks(IEnumerable<double> ticks) : base(ticks) { }
    }

    public static IReadOnlyList<double> GenerateInside(double min, double max, int desiredCount) {
        desiredCount = Math.Min(MaximumGeneratedTicks, Math.Max(2, desiredCount));
        if (!TryNormalize(ref min, ref max, out var scale, out var normalizedMin, out var normalizedMax)) return new[] { 0d, 1d };
        if (min == max) return new[] { min };

        var normalizedStep = ChartNiceNumbers.Step((normalizedMax - normalizedMin) / (desiredCount - 1));
        if ((normalizedMax - normalizedMin) / normalizedStep >= MaximumGeneratedTicks - 1)
            normalizedStep = ChartNiceNumbers.Step((normalizedMax - normalizedMin) / (MaximumGeneratedTicks - 1));
        var step = normalizedStep * scale;
        if (!IsPositiveFinite(normalizedStep) || !IsPositiveFinite(step)) return DistinctEndpoints(min, max);

        var start = Math.Ceiling(normalizedMin / normalizedStep) * normalizedStep;
        var ticks = new List<double>();
        for (var index = 0; index < desiredCount; index++) {
            var normalizedValue = start + normalizedStep * index;
            if (normalizedValue > normalizedMax) {
                // Only absorb arithmetic rounding at an exact upper boundary;
                // a fraction of the tick step can include a genuinely out-of-range value.
                var tolerance = Math.Max(Math.Abs(normalizedValue), Math.Abs(normalizedMax)) * 8 * 2.2204460492503131e-16;
                if (normalizedValue - normalizedMax > tolerance) break;
                normalizedValue = normalizedMax;
            }
            AddFiniteDistinct(ticks, Denormalize(NormalizeZero(normalizedValue, normalizedStep), scale, min, max, normalizedMin, normalizedMax));
        }

        return ticks.Count >= 2 ? ticks : DistinctEndpoints(min, max);
    }

    private static IReadOnlyList<double> GenerateTransformed(ChartAxis axis, double min, double max, int desiredCount, bool inside) {
        var transformedMin = ChartScaleTransform.Forward(min, axis);
        var transformedMax = ChartScaleTransform.Forward(max, axis);
        var transformed = inside ? GenerateInside(transformedMin, transformedMax, desiredCount) : Generate(transformedMin, transformedMax, desiredCount);
        var ticks = new List<double>(transformed.Count);
        for (var i = 0; i < transformed.Count; i++) ticks.Add(ChartScaleTransform.Inverse(transformed[i], axis));
        return ticks;
    }

    private static IReadOnlyList<double> GenerateLogarithmic(double min, double max, int desiredCount, bool inside) {
        desiredCount = Math.Min(MaximumGeneratedTicks, Math.Max(2, desiredCount));
        if (!IsPositiveFinite(min) || !IsPositiveFinite(max)) throw new InvalidOperationException("Logarithmic axes require positive data and bounds.");
        if (min > max) (min, max) = (max, min);
        var firstExponent = (int)Math.Ceiling(Math.Log10(min));
        var lastExponent = (int)Math.Floor(Math.Log10(max));
        var ticks = new List<double>();
        var exponentCount = Math.Max(0, lastExponent - firstExponent + 1);
        var outerCount = inside ? 0 : (exponentCount == 0 || Math.Pow(10, firstExponent) > min ? 1 : 0)
            + (exponentCount == 0 || Math.Pow(10, lastExponent) < max ? 1 : 0);
        if ((!inside && desiredCount == 2) || desiredCount <= outerCount) return DistinctEndpoints(min, max);
        var stride = Math.Max(1, (int)Math.Ceiling(exponentCount / (double)(desiredCount - outerCount)));
        do {
            ticks.Clear();
            for (var exponent = firstExponent; exponent <= lastExponent; exponent += stride) AddFiniteDistinct(ticks, Math.Pow(10, exponent));
            if (!inside && (ticks.Count == 0 || ticks[0] > min)) ticks.Insert(0, min);
            if (!inside && (ticks.Count == 0 || ticks[ticks.Count - 1] < max)) ticks.Add(max);
            // A stride may skip an exact upper power that was already counted as
            // an endpoint. Budget the actual completed envelope before returning.
            stride++;
        } while (ticks.Count > desiredCount);
        return ticks.Count > 0 ? ticks : DistinctEndpoints(min, max);
    }

    private static bool TryNormalize(ref double min, ref double max, out double scale, out double normalizedMin, out double normalizedMax) {
        scale = 1;
        normalizedMin = 0;
        normalizedMax = 1;
        if (!IsFinite(min) || !IsFinite(max)) return false;
        if (min > max) (min, max) = (max, min);
        if (min == max) return true;

        var directRange = max - min;
        var magnitude = Math.Max(Math.Abs(min), Math.Abs(max));
        if (IsPositiveFinite(directRange) && directRange > double.Epsilon && magnitude <= DirectMagnitudeLimit) {
            normalizedMin = min;
            normalizedMax = max;
            return true;
        }

        scale = magnitude;
        if (scale == 0) scale = 1;
        normalizedMin = min / scale;
        normalizedMax = max / scale;
        return IsFinite(normalizedMin) && IsFinite(normalizedMax) && normalizedMax > normalizedMin;
    }

    private static double Denormalize(double value, double scale, double min, double max, double normalizedMin, double normalizedMax) {
        if (scale == 1) return value;
        var result = value * scale;
        return IsFinite(result) ? result : value < normalizedMin ? min : max;
    }

    private static IReadOnlyList<double> ExpandEqualValue(double value) {
        if (value == 0) return new[] { -1d, 1d };
        var delta = Math.Abs(value) * 0.1;
        if (delta == 0) delta = double.Epsilon;
        var lower = value - delta;
        var upper = value + delta;
        if (!IsFinite(lower)) lower = value;
        if (!IsFinite(upper)) upper = value;
        return DistinctEndpoints(lower, upper);
    }

    private static IReadOnlyList<double> DistinctEndpoints(double min, double max) {
        if (min != max) return new[] { min, max };
        if (min == 0) return new[] { -1d, 1d };
        var adjacent = min > 0 ? min - Math.Abs(min) * 0.1 : min + Math.Abs(min) * 0.1;
        return adjacent == min || !IsFinite(adjacent) ? new[] { min } : min < adjacent ? new[] { min, adjacent } : new[] { adjacent, min };
    }

    private static void AddFiniteDistinct(List<double> ticks, double value) {
        if (!IsFinite(value)) return;
        if (ticks.Count == 0 || ticks[ticks.Count - 1] != value) ticks.Add(value);
    }

    private static void PreserveUpperEndpoint(List<double> ticks, double upperEndpoint, int maximumCount) {
        if (!IsFinite(upperEndpoint)) return;
        if (ticks.Count == 0) {
            ticks.Add(upperEndpoint);
            return;
        }

        if (ticks[ticks.Count - 1] >= upperEndpoint) return;
        if (ticks.Count >= maximumCount) ticks[ticks.Count - 1] = upperEndpoint;
        else ticks.Add(upperEndpoint);
    }

    private static double NormalizeZero(double value, double step) => Math.Abs(value) < step / 1_000_000 ? 0 : value;


    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static bool IsPositiveFinite(double value) => value > 0 && IsFinite(value);
}
