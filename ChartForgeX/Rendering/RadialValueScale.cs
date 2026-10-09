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

    /// <summary>Uses compiled segment endpoints when a numeric radial family supports signed baselines and stacks.</summary>
    internal static RadialValueScale Create(ChartAxis axis, IEnumerable<double> values, string chartName, bool includeZero) {
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (string.IsNullOrWhiteSpace(chartName)) throw new ArgumentException("Chart name cannot be empty.", nameof(chartName));

        var observed = values.ToArray();
        if (observed.Length == 0) throw new InvalidOperationException(chartName + " charts require at least one value.");
        if (observed.Any(value => !ChartMath.IsFinite(value))) throw new InvalidOperationException(chartName + " values must be finite.");
        if (axis.Scale == ChartScaleKind.Logarithmic && observed.Any(value => value <= 0)) {
            throw new InvalidOperationException(chartName + " charts with logarithmic value axes require positive values only.");
        }

        var signed = includeZero && axis.Scale != ChartScaleKind.Logarithmic;
        var minimum = axis.Minimum ?? (signed ? Math.Min(0, observed.Min()) : AutomaticMinimum(axis, observed));
        var maximum = axis.Maximum ?? (signed ? Math.Max(0, observed.Max()) : observed.Max());
        if (maximum <= minimum) {
            if (signed) {
                var expanded = ChartTicks.Generate(axis, minimum, minimum);
                if (!axis.Minimum.HasValue) minimum = expanded[0];
                if (!axis.Maximum.HasValue) maximum = expanded[expanded.Count - 1];
            } else maximum = axis.Scale == ChartScaleKind.Logarithmic ? minimum * 10 : minimum + 1;
        }
        if (!ChartMath.IsFinite(minimum) || !ChartMath.IsFinite(maximum) || maximum <= minimum)
            throw new InvalidOperationException(chartName + " axis bounds cannot form a finite visible domain.");

        var generatedTicks = axis.Labels.Count > 0 ? ChartTicks.ForValueAxis(axis, minimum, maximum)
            : axis.Minimum.HasValue || axis.Maximum.HasValue ? ChartTicks.ForAxis(axis, minimum, maximum) : ChartTicks.Generate(axis, minimum, maximum);
        var ticks = ChartTicks.PreserveFormatting(generatedTicks, generatedTicks.Where(tick => tick >= minimum).ToArray());
        if (ticks.Count > 0 && !axis.Maximum.HasValue) maximum = Math.Max(maximum, ticks[ticks.Count - 1]);
        return new RadialValueScale(axis, minimum, maximum, ticks);
    }

    /// <summary>Maps a radial value into the configured scale after constraining it to the visible domain.</summary>
    public double Normalize(double value) {
        var bounded = Math.Min(Maximum, Math.Max(Minimum, value));
        return ChartScaleTransform.Normalize(bounded, Minimum, Maximum, _axis);
    }

    public bool IsMaximum(double value) => ChartTicks.IsNumericTimeFallback(Ticks) ? value == Maximum : Math.Abs(value - Maximum) <= Math.Max(0.000001, Math.Abs(Maximum) * 0.000001);

    private static double AutomaticMinimum(ChartAxis axis, IReadOnlyList<double> values) {
        if (axis.Scale == ChartScaleKind.Logarithmic) {
            var smallest = values.Min();
            var baseline = smallest / 10;
            return baseline > 0 && !double.IsInfinity(baseline) ? baseline : smallest;
        }

        return axis.Maximum.HasValue && axis.Maximum.Value <= 0 ? axis.Maximum.Value - 1 : 0;
    }
}
