using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>The tick policy a saved page needs to re-tick a zoomed window the way native layout would.</summary>
internal sealed class ChartAxisTickFacts {
    private ChartAxisTickFacts(ChartAxis axis) {
        Step = axis.TickStep; Values = axis.TickValues; MinorTicks = axis.MinorTickCount;
        MinimumSpacing = axis.MinimumTickSpacing; MaximumSpacing = axis.MaximumTickSpacing;
        // Calendar steps and bands between ticks have no browser re-projection, so a saved page declines zoom for them.
        Static = axis.TimeTickStep != null || axis.Grid?.ShowBands == true;
    }

    /// <summary>Captures an axis's tick policy, or null for automatic ticks without bands.</summary>
    internal static ChartAxisTickFacts? Capture(ChartAxis axis) => axis.HasTickPolicy || axis.Grid?.ShowBands == true ? new ChartAxisTickFacts(axis) : null;
    internal double? Step { get; }
    internal IReadOnlyList<double> Values { get; }
    internal int MinorTicks { get; }
    internal double? MinimumSpacing { get; }
    internal double? MaximumSpacing { get; }
    internal bool Static { get; }
}

/// <summary>Resolves an axis's authored tick step, explicit values and minor subdivisions for one visible domain.</summary>
internal static class ChartAxisTickPolicy {
    /// <summary>The most ticks a fixed step may place across one rendered domain.</summary>
    internal const int MaximumStepTicks = 1000;

    /// <summary>Returns authored ticks for a domain, or null when the axis generates its own. Inside keeps every tick
    /// within the domain; otherwise one outer tick on each side lets automatic bounds reach the next authored tick.</summary>
    internal static IReadOnlyList<double>? Generate(ChartAxis axis, double minimum, double maximum, bool inside) {
        if (!axis.HasFixedTicks || !ChartMath.IsFinite(minimum) || !ChartMath.IsFinite(maximum)) return null;
        if (minimum > maximum) (minimum, maximum) = (maximum, minimum);
        if (axis.TickValues.Count > 0) return Values(axis.TickValues, minimum, maximum, inside);
        if (axis.TimeTickStep is { } interval) {
            var ticks = ChartTimeScale.GenerateFixed(axis, minimum, maximum, interval)
                ?? throw new InvalidOperationException("The time tick step produces more ticks than an axis can draw; choose a longer step.");
            return ticks;
        }
        var step = axis.TickStep!.Value;
        return axis.Scale == ChartScaleKind.Logarithmic ? Logarithmic(axis, minimum, maximum, step, inside) : Linear(minimum, maximum, step, inside);
    }

    /// <summary>Divides every major interval, and the partial intervals at both domain ends, into equal data-unit parts.</summary>
    internal static IReadOnlyList<double> Minor(ChartAxis axis, IReadOnlyList<double> majors, double minimum, double maximum) {
        var count = axis.MinorTickCount;
        if (count == 0 || majors.Count < 2) return Array.Empty<double>();
        if (minimum > maximum) (minimum, maximum) = (maximum, minimum);
        var result = new List<double>();
        // Partial end intervals repeat the neighbouring interval in the scale's own transformed space,
        // so a logarithmic decade below the first power subdivides like the decade above it.
        var first = ChartScaleTransform.Forward(majors[0], axis); var second = ChartScaleTransform.Forward(majors[1], axis);
        var last = ChartScaleTransform.Forward(majors[majors.Count - 1], axis); var previous = ChartScaleTransform.Forward(majors[majors.Count - 2], axis);
        Divide(ChartScaleTransform.Inverse(2 * first - second, axis), majors[0]);
        for (var index = 1; index < majors.Count; index++) Divide(majors[index - 1], majors[index]);
        Divide(majors[majors.Count - 1], ChartScaleTransform.Inverse(2 * last - previous, axis));
        return result;

        void Divide(double low, double high) {
            if (!ChartMath.IsFinite(low) || !ChartMath.IsFinite(high) || high <= low) return;
            for (var part = 1; part <= count; part++) {
                var value = low + (high - low) * part / (count + 1);
                if (value >= minimum && value <= maximum) result.Add(value);
            }
        }
    }

    private static IReadOnlyList<double> Values(IReadOnlyList<double> values, double minimum, double maximum, bool inside) {
        var result = new List<double>(values.Count);
        for (var index = 0; index < values.Count; index++) {
            var value = values[index];
            var within = value >= minimum && value <= maximum;
            var outer = !inside && (value < minimum && (index + 1 == values.Count || values[index + 1] >= minimum)
                || value > maximum && (index == 0 || values[index - 1] <= maximum));
            if (within || outer) result.Add(value);
        }
        return result;
    }

    private static IReadOnlyList<double> Linear(double minimum, double maximum, double step, bool inside) {
        var tolerance = 1e-9;
        var first = inside ? Math.Ceiling(minimum / step - tolerance) : Math.Floor(minimum / step + tolerance);
        var last = inside ? Math.Floor(maximum / step + tolerance) : Math.Ceiling(maximum / step - tolerance);
        Budget(last - first + 1);
        var ticks = new List<double>();
        for (var index = first; index <= last; index++) {
            var value = index * step;
            // Multiples of a decimal step accumulate binary error; round to the step's own precision.
            ticks.Add(index == 0 ? 0 : Clean(value, step));
        }
        return ticks;
    }

    private static IReadOnlyList<double> Logarithmic(ChartAxis axis, double minimum, double maximum, double step, bool inside) {
        if (!axis.IsInLogarithmicDomain(minimum) || !axis.IsInLogarithmicDomain(maximum))
            throw new InvalidOperationException("Logarithmic axes require " + axis.LogarithmicDomainName + " data and bounds.");
        var negative = axis.LogarithmicDomain == ChartLogarithmicDomain.Negative;
        var low = negative ? -maximum : minimum; var high = negative ? -minimum : maximum;
        var exponents = Linear(ChartScaleTransform.Logarithm(low, axis), ChartScaleTransform.Logarithm(high, axis), Math.Round(step), inside);
        var ticks = new List<double>(exponents.Count);
        foreach (var exponent in exponents) {
            var value = Math.Pow(axis.LogarithmBase, exponent);
            if (ChartMath.IsFinite(value) && value > 0) ticks.Add(negative ? -value : value);
        }
        if (negative) ticks.Reverse();
        return ticks;
    }

    private static void Budget(double count) {
        if (!(count <= MaximumStepTicks))
            throw new InvalidOperationException("The tick step places " + (ChartMath.IsFinite(count) ? count.ToString("0", System.Globalization.CultureInfo.InvariantCulture) : "too many")
                + " ticks across the axis; the limit is " + MaximumStepTicks + ". Choose a larger step.");
    }

    private static double Clean(double value, double step) {
        var digits = Math.Max(0, Math.Min(15, 15 - (int)Math.Floor(Math.Log10(Math.Max(Math.Abs(value), step)))));
        return Math.Round(value, digits);
    }
}
