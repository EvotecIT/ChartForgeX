using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Owns renderer-neutral bounds, ticks, and normalization for radial value axes.</summary>
internal sealed class RadialValueScale {
    private readonly ChartAxis _axis;

    private RadialValueScale(ChartAxis axis, double minimum, double maximum, IReadOnlyList<double> ticks) {
        _axis = axis;
        Minimum = minimum;
        Maximum = maximum;
        Ticks = ticks;
    }

    public double Minimum { get; }

    public double Maximum { get; }

    public IReadOnlyList<double> Ticks { get; }

    /// <summary>Builds one shared radial scale from the configured value axis and rendered series.</summary>
    public static RadialValueScale Create(ChartAxis axis, IEnumerable<ChartSeries> series, string chartName) {
        if (series == null) throw new ArgumentNullException(nameof(series));
        return Create(axis, series.SelectMany(item => item.Points).Select(point => point.Y), chartName, false);
    }

    /// <summary>Builds independent radius domains from each series' selected value axis.</summary>
    internal static Dictionary<ChartAxisSide, RadialValueScale> ForAxes(Chart chart, Func<int, IEnumerable<double>> observations,
        string chartName, bool includeZero = false) {
        var result = new Dictionary<ChartAxisSide, RadialValueScale>();
        foreach (var side in chart.Series.Select(series => series.ValueAxis).Distinct()) {
            var axis = Axis(chart, side);
            var values = Enumerable.Range(0, chart.Series.Count).Where(index => chart.Series[index].ValueAxis == side)
                .SelectMany(observations).Where(value => !includeZero || axis.Scale != ChartScaleKind.Logarithmic || value != 0).ToArray();
            // Empty series retain identity without inventing another axis's domain.
            if (values.Length == 0) {
                if (!axis.Minimum.HasValue || !axis.Maximum.HasValue) continue;
                values = new[] { axis.Minimum.Value, axis.Maximum.Value };
            }
            result.Add(side, Create(axis, values, chartName, includeZero));
        }
        return result;
    }

    internal static ChartAxis Axis(Chart chart, ChartAxisSide side) => side == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;

    /// <summary>Uses compiled segment endpoints when a numeric radial family supports signed baselines and stacks.</summary>
    internal static RadialValueScale Create(ChartAxis axis, IEnumerable<double> values, string chartName, bool includeZero) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (string.IsNullOrWhiteSpace(chartName)) throw new ArgumentException("Chart name cannot be empty.", nameof(chartName));

        var observed = values.ToArray();
        if (observed.Length == 0) throw new InvalidOperationException(chartName + " charts require at least one value.");
        if (observed.Any(value => !ChartMath.IsFinite(value))) throw new InvalidOperationException(chartName + " values must be finite.");
        if (axis.Scale == ChartScaleKind.Logarithmic && observed.Any(value => !axis.IsInLogarithmicDomain(value))) {
            throw new InvalidOperationException(chartName + " charts with logarithmic value axes require " + axis.LogarithmicDomainName + " values only.");
        }

        var signed = includeZero && axis.Scale != ChartScaleKind.Logarithmic;
        var minimum = axis.Minimum ?? (signed ? Math.Min(0, observed.Min()) : AutomaticMinimum(axis, observed));
        var maximum = axis.Maximum ?? (signed ? Math.Max(0, observed.Max()) : axis.Scale == ChartScaleKind.Logarithmic
            && axis.LogarithmicDomain == ChartLogarithmicDomain.Negative ? ChartRange.LogarithmicBaseline(observed.Max(), axis) : observed.Max());
        if (maximum <= minimum) {
            if (signed) {
                var expanded = ChartTicks.Generate(axis, minimum, minimum);
                if (!axis.Minimum.HasValue) minimum = expanded[0];
                if (!axis.Maximum.HasValue) maximum = expanded[expanded.Count - 1];
            } else if (axis.Scale == ChartScaleKind.Logarithmic && axis.LogarithmicDomain == ChartLogarithmicDomain.Negative) {
                minimum = maximum * axis.LogarithmBase;
            } else maximum = axis.Scale == ChartScaleKind.Logarithmic ? minimum * axis.LogarithmBase : minimum + 1;
        }
        if (!ChartMath.IsFinite(minimum) || !ChartMath.IsFinite(maximum) || maximum <= minimum)
            throw new InvalidOperationException(chartName + " axis bounds cannot form a finite visible domain.");

        var generatedTicks = axis.Labels.Count > 0 ? ChartTicks.ForValueAxis(axis, minimum, maximum)
            : axis.Minimum.HasValue || axis.Maximum.HasValue ? ChartTicks.ForAxis(axis, minimum, maximum) : ChartTicks.Generate(axis, minimum, maximum);
        if (generatedTicks.Count > 0 && !axis.Minimum.HasValue) minimum = Math.Min(minimum, generatedTicks[0]);
        var ticks = ChartTicks.PreserveFormatting(generatedTicks, generatedTicks.Where(tick => tick >= minimum).ToArray());
        if (ticks.Count > 0 && !axis.Maximum.HasValue) maximum = Math.Max(maximum, ticks[ticks.Count - 1]);
        return new RadialValueScale(axis, minimum, maximum, ticks);
    }

    /// <summary>Maps a radial value into the configured scale after constraining it to the visible domain.</summary>
    public double Normalize(double value) {
        var bounded = Math.Min(Maximum, Math.Max(Minimum, value));
        return ChartScaleTransform.Normalize(bounded, Minimum, Maximum, _axis);
    }

    /// <summary>Detaches the axis policy before a viewport adjusts the presentation copy.</summary>
    internal RadialValueScale Detached() => new(_axis.Clone(), Minimum, Maximum, Ticks);

    /// <summary>Projects an exact source-window boundary in the captured full scale, including its endpoint policy.</summary>
    internal double NormalizeBoundary(double source, bool maximum, bool maximumInclusive = true) => ChartScaleTransform.Normalize(
        ChartScaleTransform.Bound(source, _axis, maximum, maximumInclusive), ChartScaleTransform.Bound(Minimum, _axis, false),
        ChartScaleTransform.Bound(Maximum, _axis, true, maximumInclusive: true), _axis.Reversed);

    public bool IsMaximum(double value) => ChartTicks.IsNumericTimeFallback(Ticks) ? value == Maximum : Math.Abs(value - Maximum) <= Math.Max(0.000001, Math.Abs(Maximum) * 0.000001);

    /// <summary>Returns in-domain labels for equal radial intervals, including logarithmic and reversed axes.</summary>
    internal IReadOnlyList<double> EqualIntervalTicks(double count) {
        var minimum = ChartScaleTransform.Forward(Minimum, _axis);
        var maximum = ChartScaleTransform.Forward(Maximum, _axis);
        var ticks = new double[(int)Math.Floor(count)];
        for (var index = 0; index < ticks.Length; index++) {
            var ratio = (index + 1d) / count;
            if (_axis.Reversed) ratio = 1 - ratio;
            ticks[index] = ratio == 0 ? Minimum : ratio == 1 ? Maximum
                : ChartScaleTransform.Inverse(minimum * (1 - ratio) + maximum * ratio, _axis);
        }
        return ChartTicks.PreserveFormatting(Ticks, ticks);
    }

    private static double AutomaticMinimum(ChartAxis axis, IReadOnlyList<double> values) {
        if (axis.Scale == ChartScaleKind.Logarithmic) {
            var smallest = values.Min();
            return axis.LogarithmicDomain == ChartLogarithmicDomain.Negative ? smallest : ChartRange.LogarithmicBaseline(smallest, axis);
        }

        return axis.Maximum.HasValue && axis.Maximum.Value <= 0 ? axis.Maximum.Value - 1 : 0;
    }
}
