using System;
using System.Collections.Generic;

namespace ChartForgeX.Rendering;

internal static class ChartTicks {
    private const int MaximumGeneratedTicks = 10_000;
    private const double DirectMagnitudeLimit = 1e150;

    public static IReadOnlyList<double> Generate(double min, double max, int desiredCount) {
        desiredCount = Math.Min(MaximumGeneratedTicks, Math.Max(2, desiredCount));
        if (!TryNormalize(ref min, ref max, out var scale, out var normalizedMin, out var normalizedMax)) return new[] { 0d, 1d };
        if (min == max) return ExpandEqualValue(min);

        var normalizedStep = NiceNumber((normalizedMax - normalizedMin) / (desiredCount - 1), true);
        var step = normalizedStep * scale;
        if (!IsPositiveFinite(normalizedStep) || !IsPositiveFinite(step)) return DistinctEndpoints(min, max);

        var niceMin = Math.Floor(normalizedMin / normalizedStep) * normalizedStep;
        var niceMax = Math.Ceiling(normalizedMax / normalizedStep) * normalizedStep;
        var ticks = new List<double>();
        for (var index = 0; index < MaximumGeneratedTicks; index++) {
            var normalizedValue = niceMin + normalizedStep * index;
            if (normalizedValue > niceMax + normalizedStep * 0.5) break;
            AddFiniteDistinct(ticks, Denormalize(NormalizeZero(normalizedValue, normalizedStep), scale, min, max, normalizedMin, normalizedMax));
        }

        PreserveUpperEndpoint(ticks, Denormalize(NormalizeZero(niceMax, normalizedStep), scale, min, max, normalizedMin, normalizedMax));

        return ticks.Count >= 2 ? ticks : DistinctEndpoints(min, max);
    }

    public static IReadOnlyList<double> GenerateInside(double min, double max, int desiredCount) {
        desiredCount = Math.Min(MaximumGeneratedTicks, Math.Max(2, desiredCount));
        if (!TryNormalize(ref min, ref max, out var scale, out var normalizedMin, out var normalizedMax)) return new[] { 0d, 1d };
        if (min == max) return new[] { min };

        var range = max - min;
        if (IsPositiveFinite(range) && IsCloseToInteger(min) && IsCloseToInteger(max) && range <= Math.Max(6, desiredCount * 2)) {
            var integerTicks = new List<double>();
            for (var value = Math.Ceiling(min); value <= Math.Floor(max) && integerTicks.Count < MaximumGeneratedTicks; value++) integerTicks.Add(value);
            PreserveUpperEndpoint(integerTicks, max);
            return integerTicks.Count > 0 ? integerTicks : DistinctEndpoints(min, max);
        }

        var normalizedStep = NiceNumber((normalizedMax - normalizedMin) / (desiredCount - 1), true);
        var step = normalizedStep * scale;
        if (!IsPositiveFinite(normalizedStep) || !IsPositiveFinite(step)) return DistinctEndpoints(min, max);

        var start = Math.Ceiling(normalizedMin / normalizedStep) * normalizedStep;
        var ticks = new List<double>();
        if (Math.Abs(start - normalizedMin) > normalizedStep * 0.2) ticks.Add(min);
        for (var index = 0; index < MaximumGeneratedTicks; index++) {
            var normalizedValue = start + normalizedStep * index;
            if (normalizedValue > normalizedMax + normalizedStep * 0.001) break;
            AddFiniteDistinct(ticks, Denormalize(NormalizeZero(normalizedValue, normalizedStep), scale, min, max, normalizedMin, normalizedMax));
        }

        if (ticks.Count == 0 || Math.Abs(ticks[ticks.Count - 1] - max) > step * 0.2) PreserveUpperEndpoint(ticks, max);
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
        if (value <= normalizedMin) return min;
        if (value >= normalizedMax) return max;
        return value * scale;
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

    private static void PreserveUpperEndpoint(List<double> ticks, double upperEndpoint) {
        if (!IsFinite(upperEndpoint)) return;
        if (ticks.Count == 0) {
            ticks.Add(upperEndpoint);
            return;
        }

        if (ticks[ticks.Count - 1] >= upperEndpoint) return;
        if (ticks.Count >= MaximumGeneratedTicks) ticks[ticks.Count - 1] = upperEndpoint;
        else ticks.Add(upperEndpoint);
    }

    private static double NormalizeZero(double value, double step) => Math.Abs(value) < step / 1_000_000 ? 0 : value;

    private static double NiceNumber(double value, bool round) {
        if (!IsPositiveFinite(value)) return double.NaN;
        var exponent = Math.Floor(Math.Log10(value));
        var power = Math.Pow(10, exponent);
        if (!IsPositiveFinite(power)) return double.NaN;
        var fraction = value / power;
        double niceFraction;

        if (round) {
            if (fraction < 1.5) niceFraction = 1;
            else if (fraction < 3) niceFraction = 2;
            else if (fraction < 7) niceFraction = 5;
            else niceFraction = 10;
        } else {
            if (fraction <= 1) niceFraction = 1;
            else if (fraction <= 2) niceFraction = 2;
            else if (fraction <= 5) niceFraction = 5;
            else niceFraction = 10;
        }

        return niceFraction * power;
    }

    private static bool IsCloseToInteger(double value) => Math.Abs(value - Math.Round(value)) < 0.000001;

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static bool IsPositiveFinite(double value) => value > 0 && IsFinite(value);
}
