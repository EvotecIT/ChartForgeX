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
        if (axis == null) throw new ArgumentNullException(nameof(axis));
        if (series == null) throw new ArgumentNullException(nameof(series));
        if (string.IsNullOrWhiteSpace(chartName)) throw new ArgumentException("Chart name cannot be empty.", nameof(chartName));

        var values = series.SelectMany(item => item.Points).Select(point => point.Y).ToArray();
        if (values.Length == 0) throw new InvalidOperationException(chartName + " charts require at least one value.");
        if (axis.Scale == ChartScaleKind.Logarithmic && values.Any(value => value <= 0)) {
            throw new InvalidOperationException(chartName + " charts with logarithmic value axes require positive values only.");
        }

        var minimum = axis.Minimum ?? AutomaticMinimum(axis, values);
        var maximum = axis.Maximum ?? values.Max();
        if (maximum <= minimum) maximum = axis.Scale == ChartScaleKind.Logarithmic ? minimum * 10 : minimum + 1;

        var ticks = ChartTicks.Generate(axis, minimum, maximum).Where(tick => tick >= minimum).ToArray();
        if (ticks.Length > 0) maximum = Math.Max(maximum, ticks[ticks.Length - 1]);
        return new RadialValueScale(axis, minimum, maximum, ticks);
    }

    /// <summary>Maps a radial value into the configured scale after constraining it to the visible domain.</summary>
    public double Normalize(double value) {
        var bounded = Math.Min(Maximum, Math.Max(Minimum, value));
        return ChartScaleTransform.Normalize(bounded, Minimum, Maximum, _axis);
    }

    public bool IsMaximum(double value) => Math.Abs(value - Maximum) <= Math.Max(0.000001, Math.Abs(Maximum) * 0.000001);

    private static double AutomaticMinimum(ChartAxis axis, IReadOnlyList<double> values) {
        if (axis.Scale == ChartScaleKind.Logarithmic) {
            var smallest = values.Min();
            var baseline = smallest / 10;
            return baseline > 0 && !double.IsInfinity(baseline) ? baseline : smallest;
        }

        return axis.Maximum.HasValue && axis.Maximum.Value <= 0 ? axis.Maximum.Value - 1 : 0;
    }
}
