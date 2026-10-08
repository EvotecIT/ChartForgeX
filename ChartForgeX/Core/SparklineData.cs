using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>Controls whether missing observations break a line or connect the surrounding observations.</summary>
public enum ChartMissingDataPolicy {
    /// <summary>Start a new line or area segment after one or more missing observations.</summary>
    Gap,
    /// <summary>Connect the surrounding observations without changing their original horizontal positions.</summary>
    Connect
}

/// <summary>A detached sequence of compact-chart sample slots, where null is missing and zero is a real observation.</summary>
public sealed class SparklineData {
    /// <summary>Copies sample slots and resolves a shared numeric domain. All-missing data uses zero through one.</summary>
    public SparklineData(IEnumerable<double?> values, double? minimum = null, double? maximum = null,
        ChartMissingDataPolicy missingDataPolicy = ChartMissingDataPolicy.Gap, bool includeZero = false) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        ChartSamplePoints.ValidatePolicy(missingDataPolicy);
        ValidateBound(minimum, nameof(minimum)); ValidateBound(maximum, nameof(maximum));
        if (minimum.HasValue && maximum.HasValue && maximum.Value <= minimum.Value)
            throw new ArgumentOutOfRangeException(nameof(maximum), "Maximum must be greater than minimum.");
        var slots = new List<double?>();
        foreach (var value in values) {
            ValidateBound(value, nameof(values)); slots.Add(value);
        }
        if (slots.Count == 0) throw new ArgumentException("At least one sample slot is required.", nameof(values));
        Values = slots.AsReadOnly(); MissingDataPolicy = missingDataPolicy;
        RequestedMinimum = minimum; RequestedMaximum = maximum; IncludeZero = includeZero;
        var domain = ResolveBounds(Values, minimum, maximum, includeZero);
        Minimum = domain.Minimum; Maximum = domain.Maximum;
    }

    /// <summary>Gets original sample slots, including missing leading, trailing and interior observations.</summary>
    public IReadOnlyList<double?> Values { get; }
    /// <summary>Gets the missing-observation connection policy.</summary>
    public ChartMissingDataPolicy MissingDataPolicy { get; }
    /// <summary>Gets the resolved finite lower display bound.</summary>
    public double Minimum { get; }
    /// <summary>Gets the resolved finite upper display bound.</summary>
    public double Maximum { get; }
    /// <summary>Gets whether automatic bounds include zero. Explicit bounds take precedence.</summary>
    public bool IncludeZero { get; }
    internal double? RequestedMinimum { get; }
    internal double? RequestedMaximum { get; }

    /// <summary>Copies finite observations into sample slots without introducing missing values.</summary>
    public static SparklineData FromValues(IEnumerable<double> values, double? minimum = null, double? maximum = null, bool includeZero = false) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var slots = new List<double?>();
        foreach (var value in values) slots.Add(value);
        return new SparklineData(slots, minimum, maximum, includeZero: includeZero);
    }

    /// <summary>Creates finite points with one-based sample positions and explicit segment breaks.</summary>
    public ChartPoint[] ToPoints() => ChartSamplePoints.FromValues(Values, MissingDataPolicy);

    internal double Ratio(double value) {
        var span = Maximum - Minimum;
        var ratio = double.IsInfinity(span)
            ? (value / 2 - Minimum / 2) / (Maximum / 2 - Minimum / 2)
            : (value - Minimum) / span;
        return Math.Max(0, Math.Min(1, ratio));
    }

    private static (double Minimum, double Maximum) ResolveBounds(IReadOnlyList<double?> values, double? minimum, double? maximum, bool includeZero) {
        double? observedMinimum = includeZero ? 0 : null, observedMaximum = includeZero ? 0 : null;
        foreach (var value in values) {
            if (!value.HasValue) continue;
            observedMinimum = Math.Min(observedMinimum ?? value.Value, value.Value);
            observedMaximum = Math.Max(observedMaximum ?? value.Value, value.Value);
        }
        var lower = minimum ?? observedMinimum ?? 0;
        var upper = maximum ?? observedMaximum ?? (minimum.HasValue ? lower : 1);
        if (upper > lower) return (lower, upper);
        var step = Math.Max(1, Math.Abs(lower) * 0.05);
        if (minimum.HasValue && !maximum.HasValue) {
            upper = lower + step;
            if (double.IsInfinity(upper)) throw new ArgumentOutOfRangeException(nameof(minimum), "The minimum leaves no finite upper display bound.");
        } else if (maximum.HasValue || double.IsInfinity(lower + step)) {
            lower = upper - Math.Max(1, Math.Abs(upper) * 0.05);
            if (double.IsInfinity(lower)) lower = -double.MaxValue;
        } else upper = lower + step;
        if (upper <= lower || double.IsNaN(lower) || double.IsNaN(upper))
            throw new ArgumentOutOfRangeException(nameof(minimum), "The bounds must leave a finite non-empty display domain.");
        return (lower, upper);
    }

    private static void ValidateBound(double? value, string name) {
        if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value)))
            throw new ArgumentOutOfRangeException(name, "Observations and bounds must be finite; use null for missing data.");
    }
}

/// <summary>The single nullable-observation ingestion owner for compact and full charts.</summary>
internal static class ChartSamplePoints {
    internal static ChartPoint[] FromValues(IEnumerable<double?> values, ChartMissingDataPolicy policy) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var x = new List<double>(); var y = new List<double?>(); var index = 1d;
        foreach (var value in values) { x.Add(index++); y.Add(value); }
        return FromXY(x, y, policy);
    }

    internal static ChartPoint[] FromXY(IEnumerable<double> x, IEnumerable<double?> y, ChartMissingDataPolicy policy) {
        if (x == null) throw new ArgumentNullException(nameof(x));
        if (y == null) throw new ArgumentNullException(nameof(y));
        ValidatePolicy(policy);
        var points = new List<ChartPoint>(); var missing = false; var count = 0;
        using (var horizontal = x.GetEnumerator())
        using (var vertical = y.GetEnumerator()) {
            while (true) {
                var hasX = horizontal.MoveNext(); var hasY = vertical.MoveNext();
                if (hasX != hasY) throw new ArgumentException("X and Y must have the same number of sample slots.", nameof(y));
                if (!hasX) break;
                count++;
                if (double.IsNaN(horizontal.Current) || double.IsInfinity(horizontal.Current))
                    throw new ArgumentOutOfRangeException(nameof(x), "Horizontal positions must be finite, including missing sample slots.");
                if (!vertical.Current.HasValue) { missing = true; continue; }
                points.Add(new ChartPoint(horizontal.Current, vertical.Current.Value, missing && policy == ChartMissingDataPolicy.Gap && points.Count > 0));
                missing = false;
            }
        }
        if (count == 0) throw new ArgumentException("At least one sample slot is required.", nameof(y));
        return points.ToArray();
    }

    internal static void ValidatePolicy(ChartMissingDataPolicy policy) {
        if (!Enum.IsDefined(typeof(ChartMissingDataPolicy), policy)) throw new ArgumentOutOfRangeException(nameof(policy));
    }
}
