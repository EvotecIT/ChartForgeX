using System;
using System.Collections.Generic;

namespace ChartForgeX.Rendering;

internal static class ChartTicks {
    public static IReadOnlyList<double> Generate(double min, double max, int desiredCount) {
        desiredCount = Math.Max(2, desiredCount);
        if (double.IsNaN(min) || double.IsNaN(max) || double.IsInfinity(min) || double.IsInfinity(max)) {
            return new[] { 0d, 1d };
        }

        if (Math.Abs(max - min) < double.Epsilon) {
            max = min + 1;
        }

        var step = NiceNumber((max - min) / (desiredCount - 1), true);
        var niceMin = Math.Floor(min / step) * step;
        var niceMax = Math.Ceiling(max / step) * step;
        var ticks = new List<double>();

        for (var value = niceMin; value <= niceMax + step * 0.5; value += step) {
            ticks.Add(Math.Abs(value) < step / 1_000_000 ? 0 : value);
        }

        return ticks.Count >= 2 ? ticks : new[] { niceMin, niceMax };
    }

    public static IReadOnlyList<double> GenerateInside(double min, double max, int desiredCount) {
        desiredCount = Math.Max(2, desiredCount);
        if (double.IsNaN(min) || double.IsNaN(max) || double.IsInfinity(min) || double.IsInfinity(max)) {
            return new[] { 0d, 1d };
        }

        if (Math.Abs(max - min) < double.Epsilon) {
            return new[] { min };
        }

        if (IsCloseToInteger(min) && IsCloseToInteger(max) && max - min <= Math.Max(6, desiredCount * 2)) {
            var integerTicks = new List<double>();
            for (var value = (int)Math.Ceiling(min); value <= (int)Math.Floor(max); value++) {
                integerTicks.Add(value);
            }

            return integerTicks.Count > 0 ? integerTicks : new[] { min, max };
        }

        var step = NiceNumber((max - min) / (desiredCount - 1), true);
        var start = Math.Ceiling(min / step) * step;
        var ticks = new List<double>();

        if (Math.Abs(start - min) > step * 0.2) ticks.Add(min);
        for (var value = start; value <= max + step * 0.001; value += step) {
            ticks.Add(Math.Abs(value) < step / 1_000_000 ? 0 : value);
        }
        if (ticks.Count == 0 || Math.Abs(ticks[ticks.Count - 1] - max) > step * 0.2) ticks.Add(max);

        return ticks;
    }

    private static double NiceNumber(double value, bool round) {
        var exponent = Math.Floor(Math.Log10(value));
        var fraction = value / Math.Pow(10, exponent);
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

        return niceFraction * Math.Pow(10, exponent);
    }

    private static bool IsCloseToInteger(double value) => Math.Abs(value - Math.Round(value)) < 0.000001;
}
